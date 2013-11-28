

drop procedure if exists CalcBusToStopTime;
DELIMITER $$
create procedure CalcBusToStopTime(IN stopName varchar(100), IN routeNumber varchar(10),
									OUT TimeToStopSecAsc int, OUT TimeToStopSecDesc int, OUT busIDAsc int, out busIDDesc int,
									OUT EndBusStopAsc varchar(100), OUT EndBusStopDesc varchar(100))
BEGIN

	#Ascending routes meaning that the routepoints and bustops are taken from first to last ID on their respective tables.
	#Descending routes meaning that the routepoints and busstoops are from from last to first ID on their respective tables.
	declare ClosestEndPointIdAsc int;
	declare ClosestEndPointIdDesc int;
	declare ClosestBusIdAsc int;
	declare ClosestBusIdDesc int;
	declare ClosestBusDistanceAsc float;
	declare ClosestBusDistanceDesc float;
	declare ClosestBusLatAsc decimal(20,15);
	declare ClosestBusLonAsc decimal(20,15);
	declare ClosestBusLatDesc decimal(20,15);
	declare ClosestBusLonDesc decimal(20,15);
	declare ClosestBusSpeedAsc float;
	declare ClosestBusSpeedDesc float;
	
	
	drop temporary table if exists possibleRoutes;
	create temporary table possibleRoutes(
		possRouteID int,
		possRouteStopID int
	);

	#Get possible BusRoutes from routeNumber. Used when a route has subroutes.
	insert into possibleRoutes
	select distinct BusRoute.ID, BusRoute_RoutePoint.ID from BusRoute
	join BusRoute_BusStop on BusRoute.ID = BusRoute_BusStop.fk_BusRoute
	join BusStop on BusRoute_BusStop.fk_BusStop = BusStop.ID
	join BusRoute_RoutePoint on BusStop.fk_RoutePoint = BusRoute_RoutePoint.fk_RoutePoint
	where BusRoute.RouteNumber = routeNumber and BusStop.StopName = stopName;

	#Closests ascending and descending bus processes. These processes also calculates the Closests Endpoint 
	#and the distance from the closests bus to the busstop. This is calculated using the possibleRoutes table.
	call GetClosestBusAscProc(@ClosestEndEPIdAsc, @ClosestBDIstAsc, @ClosestBIDAsc );
	select @ClosestsEndPointIDAsc, @ClosestBDIstAsc, @ClosestBIDAsc 
	into ClosestEndPointIdAsc,ClosestBusDistanceAsc,ClosestBusIdAsc;
	
	Call GetClosestBusDescProc(@ClosestEndEPIdDesc, @ClosestBDIstDesc, @ClosestBIDDesc ); 
	select @ClosestEndEPIdDesc, @ClosestBDIstDesc, @ClosestBIDDesc 
	into ClosestEndPointIdDesc,ClosestBusDistanceDesc,ClosestBusIdDesc;
	
	#Calculates the average speed of the bus.
	select CalcBusAvgSpeedAsc(ClosestBusIdAsc) into ClosestBusSpeedAsc;
	select CalcBusAvgSpeedDesc(ClosestBusIdDesc) into ClosestBusSpeedDesc;

	#Time to stop = meters / meters / second = seconds
	set TimeToStopSecAsc = ClosestBusDistanceAsc/ClosestBusSpeedAsc;
	set TimeToStopSecDesc = ClosestBusDistanceDesc/ClosestBusSpeedDesc;
	set busIDAsc = ClosestBusIdAsc;
	set busIDDesc = ClosestBusIdDesc;

	#Gets the name of the busstops, that the two closests busses are going towards (both ways).
	select BusStop.StopName from BusStop 
	inner join BusRoute_BusStop on BusRoute_BusStop.fk_BusStop = BusStop.ID
	inner join Bus on BusRoute_BusStop.fk_BusRoute = Bus.fk_BusRoute
	where Bus.ID = ClosestBusIdAsc Order by BusRoute_BusStop.ID desc limit 1 into EndBusStopAsc;

	select BusStop.StopName from BusStop 
	inner join BusRoute_BusStop on BusRoute_BusStop.fk_BusStop = BusStop.ID
	inner join Bus on BusRoute_BusStop.fk_BusRoute = Bus.fk_BusRoute
	where Bus.ID = ClosestBusIdDesc Order by BusRoute_BusStop.ID asc limit 1 into EndBusStopDesc;



	drop temporary table possibleRoutes;

END$$

#Calculates the closests bus, by iterating through the number of busses, finding the closests RoutePoint for that bus, 
#and calculating the distance from the bus to that endpoint. This procedure is for busses that drives ascending.
delimiter $$
drop procedure if exists GetClosestBusAscProc $$
create procedure GetClosestBusAscProc(OUT busClosestEndPointAsc int, Out routeLengthAsc float, OUT closestBusId int)
begin 
	declare NumberOfBusses int default 0;
	declare BusCounter int default 1;
	declare currentBusId int;
	declare currentStopId int;
	declare closestbID int;
	declare closestEndPoint int;
	declare busPos_lon decimal(20,15);
	declare busPos_lat decimal(20,15);
	declare currentBusDist float default 0;
	declare leastBusDist float default 1000000000;
	declare closestEP int;

	#Table contains all busses that are driving ascending.
	drop temporary table if exists BussesOnRouteAsc;
	create temporary table BussesOnRouteAsc(
		autoId int auto_increment primary key,
		busId int,
		stopID int
	);

	#Get all possible busses that drives from first to last stop. the busstop ID is the ID from the BusRoute_RoutePoint table.
	insert into BussesOnRouteAsc (busId, stopID) select distinct Bus.ID, possibleRoutes.possRouteStopID from Bus
	inner join possibleRoutes on Bus.fk_BusRoute = possibleRoutes.possRouteID
	where Bus.IsDescending=false;
	
	#Get total number of busses.
	select count(busId) from BussesOnRouteAsc into NumberOfBusses;	
	

	#Iterate though busses.
	while BusCounter <= NumberOfBusses do
		#busID and stopID for current bus.
		select busId,stopID from BussesOnRouteAsc where autoId = BusCounter into currentBusId, currentStopId;
		#Closests endpoint retunrs the ID of the RoutePoint, the bus is closests to.
		select GetClosestEndpointAsc(currentBusId) into closestEndPoint; 
		#Closests Route point is less than or equal to the stop id. Checks to see if the bus has driven past the stop.
		#if it fails, dist is set to a high number. If no busses are valid, then the high number will be returned, but this is handled on the server.
		if(closestEndPoint <= currentStopId) then
			#Latests position of the current bus.
			select GPSPosition.Latitude, GPSPosition.Longitude from GPSPosition where GPSPosition.fk_Bus=currentBusId
			order by GPSPosition.ID desc limit 1 into busPos_lat, busPos_lon; 
			#Calculate distance from bus to stop.
			select CalcRouteLengthAsc(busPos_lon, busPos_lat, closestEndPoint, currentStopId) into currentBusDist;
		else
			set currentBusDist = 10000000;
		end if;
		#If the distance from the current bus to the busstop is lesser than the one before.
		#Change closests bus, routepoint, and distance.
		if (currentBusDist < leastBusDist) then
			set leastBusDist = currentBusDist;
			set closestbID = currentBusId;
			set closestEP = closestEndPoint;
		end if;
		set BusCounter = BusCounter + 1;
	end while;
	set busClosestEndPointAsc = closestEP;
	set routeLengthAsc = leastBusDist;
	set closestBusId = closestbID;
	
	drop temporary table BussesOnRouteAsc;
	#return bus with closests distance
END $$

#Calculates the closests bus, by iterating through the number of busses, finding the closests RoutePoint for that bus, 
#and calculating the distance from the bus to that endpoint. This procedure is for busses that drives descending.
#The same happens as in the Ascending procedure, however, since the route travels in the opposite direction, it check if then
#ID of the routepoint is <= the id of the busstop.
delimiter $$
drop procedure if exists GetClosestBusDescProc $$
create procedure GetClosestBusDescProc(OUT busClosestEndPointDesc int, Out routeLengthDesc float, OUT closestBusId int)
begin 
	declare NumberOfBusses int default 0;
	declare BusCounter int default 1;
	declare currentBusId int;
	declare currentStopId int;
	declare closestbID int;
	declare closestEndPoint int;
	declare busPos_lon decimal(20,15);
	declare busPos_lat decimal(20,15);
	declare currentBusDist float default 0;
	declare leastBusDist float default 1000000000;
	declare closestEP int;

	drop temporary table if exists BussesOnRouteDesc;
	create temporary table BussesOnRouteDesc(
		autoId int auto_increment primary key,
		busId int,
		stopId int
	);

	#Get all busses on route that drives a from last to first stop
	insert into BussesOnRouteDesc (busId,stopId) select distinct Bus.ID, possibleRoutes.possRouteStopID from Bus
	inner join possibleRoutes on Bus.fk_BusRoute = possibleRoutes.possRouteID
	where Bus.IsDescending=true;

	#Get number of busses
	select count(busId) from BussesOnRouteDesc into NumberOfBusses;	
	#Iterate though busses finding the closest bus.
	while BusCounter <= NumberOfBusses do
		select busId,stopId from BussesOnRouteDesc where autoId = BusCounter into currentBusId,currentStopId;
		#Get closests endpoint for that bus busses
		select GetClosestEndpointDesc(currentBusId) into closestEndPoint; 
		if(closestEndPoint >= currentStopId) then
			
			#Get latest latitude and longitude of current bus
			select GPSPosition.Latitude, GPSPosition.Longitude from GPSPosition where GPSPosition.fk_Bus=currentBusId
			order by GPSPosition.ID desc limit 1 into busPos_lat, busPos_lon; 
			#Calculate distance from bus to stop.
			select CalcRouteLengthDesc(busPos_lon, busPos_lat, closestEndPoint, currentStopId) into currentBusDist;
		else
			set currentBusDist = 10000000;
		end if;
		#If the distance from the current bus to the busstop is lesser than the one before.
		#Change closests bus.
		if (currentBusDist < leastBusDist) then
			set leastBusDist = currentBusDist;
			set closestbID = currentBusId;
			set closestEP = closestEndPoint;
		end if;
		set BusCounter = BusCounter + 1;
	end while;
	set busClosestEndPointDesc = closestEP;
	set routeLengthDesc = leastBusDist;
	set closestBusId = closestbID;
	drop temporary table BussesOnRouteDesc;

END $$







