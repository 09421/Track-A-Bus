

drop procedure if exists CalcBusToStopTime;
DELIMITER $$
create procedure CalcBusToStopTime(IN stopName varchar(100), IN routeNumber varchar(10),
									OUT TimeToStopSecAsc int, OUT TimeToStopSecDesc int, OUT busIDAsc int, out busIDDesc int,
									OUT EndBusStopAsc varchar(100), OUT EndBusStopDesc varchar(100))
BEGIN
	declare RouteId int;
	declare StopId int;
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

	#Get possible BusRoutes from routeNumber
	insert into possibleRoutes
	select distinct BusRoute.ID, BusRoute_RoutePoint.ID from BusRoute
	join BusRoute_BusStop on BusRoute.ID = BusRoute_BusStop.fk_BusRoute
	join BusStop on BusRoute_BusStop.fk_BusStop = BusStop.ID
	join BusRoute_RoutePoint on BusStop.fk_RoutePoint = BusRoute_RoutePoint.fk_RoutePoint
	where BusRoute.RouteNumber = routeNumber and BusStop.StopName = stopName;

	#Closests bus, both ways.
	call GetClosestBusAscProc(@ClosestEndEPIdAsc, @ClosestBDIstAsc, @ClosestBIDAsc );
	select @ClosestsEndPointIDAsc, @ClosestBDIstAsc, @ClosestBIDAsc 
	into ClosestEndPointIdAsc,ClosestBusDistanceAsc,ClosestBusIdAsc;

	Call GetClosestBusDescProc(@ClosestEndEPIdDesc, @ClosestBDIstDesc, @ClosestBIDDesc ); 
	select @ClosestEndEPIdDesc, @ClosestBDIstDesc, @ClosestBIDDesc 
	into ClosestEndPointIdDesc,ClosestBusDistanceDesc,ClosestBusIdDesc;


	select CalcBusAvgSpeedAsc(ClosestBusIdAsc) into ClosestBusSpeedAsc;
	select CalcBusAvgSpeedDesc(ClosestBusIdDesc) into ClosestBusSpeedDesc;



	set TimeToStopSecAsc = ClosestBusDistanceAsc/ClosestBusSpeedAsc;
	set TimeToStopSecDesc = ClosestBusDistanceDesc/ClosestBusSpeedDesc;
	set busIDAsc = ClosestBusIdAsc;
	set busIDDesc = ClosestBusIdDesc;

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


	drop temporary table if exists BussesOnRouteAsc;
	create temporary table BussesOnRouteAsc(
		autoId int auto_increment primary key,
		busId int,
		stopID int
	);

	#Get all busses on route that drives a from first to last stop
	insert into BussesOnRouteAsc (busId, stopID) select distinct Bus.ID, possibleRoutes.possRouteStopID from Bus
	inner join possibleRoutes on Bus.fk_BusRoute = possibleRoutes.possRouteID
	where Bus.IsDescending=false;
	
	#Get number of busses on route.
	select count(busId) from BussesOnRouteAsc into NumberOfBusses;	
	
	insert into test1 (testID) values (NumberOfBusses);

	#Iterate though busses on that route, finding the closest bus.
	while BusCounter <= NumberOfBusses do
		select busId,stopID from BussesOnRouteAsc where autoId = BusCounter into currentBusId, currentStopId;
		#Get closests endpoint for those busses
		select GetClosestEndpointAsc(currentBusId) into closestEndPoint; 
		#Get latest latitude and longitude of current bus
		if(closestEndPoint <= currentStopId) then
			select GPSPosition.Latitude, GPSPosition.Longitude from GPSPosition where GPSPosition.fk_Bus=currentBusId
			order by GPSPosition.ID desc limit 1 into busPos_lat, busPos_lon; 
			#Calculate distance from bus to stop.
			select CalcRouteLengthAsc(busPos_lon, busPos_lat, closestEndPoint, currentStopId) into currentBusDist;
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
	set busClosestEndPointAsc = closestEP;
	set routeLengthAsc = leastBusDist;
	set closestBusId = closestbID;
	
	drop temporary table BussesOnRouteAsc;
	#return bus with closests distance
END $$

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

	#Get number of busses on route.
	select count(busId) from BussesOnRouteDesc into NumberOfBusses;	
	#Iterate though busses on that route, finding the closest bus.
	while BusCounter <= NumberOfBusses do
		select busId,stopId from BussesOnRouteDesc where autoId = BusCounter into currentBusId,currentStopId;
		#Get closests endpoint for those busses
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
	#return bus with closests distance
END $$


delimiter $$
drop function if exists GetClosestEndpointAsc $$
create function GetClosestEndpointAsc(busID int)
returns int
begin
	declare RouteCounter int default 1;
	declare TotalRouteLines int;
	declare BusLastPosLon decimal(20,15);
	declare BusLastPosLat decimal(20,15);
	declare R1x decimal(20,15);
	declare R1y decimal(20,15);
	declare R2x decimal(20,15);
	declare R2y decimal(20,15);
	declare BusDist float ;
	#initialize to high number, so first endpoint will be closer,
	declare PrevBusDist float default 10000000;
	declare ClosestEndPointId int;
	declare LastChosenID int;

	drop temporary table if exists ChosenRouteAsc;
	create TEMPORARY table if not exists ChosenRouteAsc(
		id int primary key,
		bus_lat decimal(20,15),
		bus_lon decimal(20,15)
	);

	#Get route points of chosen route.


	insert into ChosenRouteAsc (id,bus_lat,bus_lon)
	select BusRoute_RoutePoint.ID, RoutePoint.Latitude,  RoutePoint.Longitude from RoutePoint
	inner join BusRoute_RoutePoint on BusRoute_RoutePoint.fk_RoutePoint = RoutePoint.ID
	inner join Bus on Bus.fk_BusRoute = BusRoute_RoutePoint.fk_BusRoute
	where Bus.ID = busID
	order by(BusRoute_RoutePoint.ID) asc;

	select ChosenRouteAsc.ID from ChosenRouteAsc order by id desc limit 1 into LastChosenID;
	select ChosenRouteAsc.ID from ChosenRouteAsc order by id asc limit 1 into RouteCounter;
	select count(id) from ChosenRouteAsc into TotalRouteLines;


	#Itereate throught endpoints, finding the endpoint closest to the bus
	#Only lesser than, because last point in list can only be endpoint.

	select GPSPosition.Latitude,  GPSPosition.Longitude from GPSPosition where GPSPosition.fk_Bus = busID
	order by GPSPosition.ID desc limit 1 into BusLastPosLat, BusLastPosLon;
	while RouteCounter < TotalRouteLines + LastChosenID do
		select bus_lon from ChosenRouteAsc where id = RouteCounter into R1x;
		select bus_lat from ChosenRouteAsc where id = RouteCounter into R1y;
		select bus_lon from ChosenRouteAsc where id = RouteCounter+1 into R2x;
		select bus_lat from ChosenRouteAsc where id = RouteCounter+1 into R2y;
		set BusDist = CalcRouteLineDist(BusLastPosLon, BusLastPosLat, R1x, R1y, R2x, R2y);
		if BusDist < PrevBusDist then
			set PrevBusDist = BusDist;
			set ClosestEndPointId = RouteCounter+1;
		end if;
		Set RouteCounter = RouteCounter + 1;
	end while;
return ClosestEndPointId;
END$$

delimiter $$
drop function if exists GetClosestEndpointDesc $$
create function GetClosestEndpointDesc (busID int)
returns int
begin
	declare RouteCounter int;
	declare TotalRouteLines int;
	declare BusLastPosLon decimal(20,15);
	declare BusLastPosLat decimal(20,15);
	declare R1x decimal(20,15);
	declare R1y decimal(20,15);
	declare R2x decimal(20,15);
	declare R2y decimal(20,15);
	declare BusDist float ;
	#initialize to high number, so first endpoint will be closer,
	declare PrevBusDist float default 10000000;
	declare ClosestEndPointId int;
	declare LastChosenID int;


	drop temporary table if exists ChosenRouteDesc;
	create TEMPORARY table if not exists ChosenRouteDesc (
		id int primary key,
		bus_lat decimal(20,15),
		bus_lon decimal(20,15)
	);

	#Get route points of chosen route.


	insert into ChosenRouteDesc  (id,bus_lat,bus_lon)
	select BusRoute_RoutePoint.ID, RoutePoint.Latitude,  RoutePoint.Longitude from RoutePoint
	inner join BusRoute_RoutePoint on BusRoute_RoutePoint.fk_RoutePoint = RoutePoint.ID
	inner join Bus on Bus.fk_BusRoute = BusRoute_RoutePoint.fk_BusRoute
	where Bus.ID = busID
	order by(BusRoute_RoutePoint.ID) desc;

	select ChosenRouteDesc.ID from ChosenRouteDesc order by id asc limit 1 into LastChosenID;
	select ChosenRouteDesc.ID from ChosenRouteDesc order by id desc limit 1 into RouteCounter;
	
	select count(id) from ChosenRouteDesc  into TotalRouteLines;
	#Itereate throught endpoints, finding the endpoint closest to the bus
	#Only lesser than, because last point in list can only be endpoint.

	select GPSPosition.Latitude,  GPSPosition.Longitude from GPSPosition where GPSPosition.fk_Bus = busID
	order by GPSPosition.ID desc limit 1 into BusLastPosLat, BusLastPosLon;

	while RouteCounter >  LastChosenID - TotalRoutelines do
		select bus_lon from ChosenRouteDesc where id = RouteCounter into R1x;
		select bus_lat from ChosenRouteDesc where id = RouteCounter into R1y;
		select bus_lon from ChosenRouteDesc where id = RouteCounter-1 into R2x;
		select bus_lat from ChosenRouteDesc where id = RouteCounter-1 into R2y;
		set BusDist = CalcRouteLineDist(BusLastPosLon, BusLastPosLat, R1x, R1y, R2x, R2y);

		if BusDist < PrevBusDist then
			set PrevBusDist = BusDist;
			set ClosestEndPointId = RouteCounter-1;
		end if;
		Set RouteCounter = RouteCounter - 1;
	end while;
return ClosestEndPointId;
END$$


delimiter $$
drop function if exists CalcRouteLineDist $$
create function CalcRouteLineDist(bus_pos_lon_x decimal(20,15), bus_pos_lat_y decimal(20,15), 
									rx_start decimal(20,15), ry_start decimal(20,15),
									rx_end decimal(20,15), ry_end decimal(20,15)) 
returns float
begin
	declare rA decimal(20,15); 
	declare rB decimal(20,15);
	declare bcx decimal(20,15);
	declare bcy decimal(20,15);
	declare dist float;

	
	

	if rx_end - rx_start = 0 then
		set dist = ABS(bus_pos_lon_x - rx_start);
		set bcx = rx_start;
		set bcy = bus_pos_lat_y;
	elseif ry_end - ry_start = 0 then
		set dist = ABS(bus_pos_lat_y - ry_start);
		set bcx = bus_pos_lon_x;
		set bcy = ry_start;
	else
		set rA = (ry_end - ry_start) / (rx_end - rx_start);
		set rB = ry_start + (rA*(-rx_start));
		set dist = ABS(bus_pos_lat_y - (rA * bus_pos_lon_x) - rB) / sqrt((rA * rA) + 1);
	end if;

	set bcx = ((rA * bus_pos_lat_y) + bus_pos_lon_x - (rA * rB)) / ((rA*rA) + 1);
	set bcy = ((rA*rA*bus_pos_lat_y) + (rA * bus_pos_lon_x) + rB) / ((rA*rA)+ 1);
	if (bcx > rx_start AND bcx > rx_end) or (bcx < rx_start AND bcx < rx_end) or
	   (bcy > ry_start AND bcy > ry_end) or (bcy < ry_start AND bcy < ry_end) then
		return 1000000;
	end if;
	
	return dist;
end$$

delimiter $$
drop function if exists CalcRouteLengthAsc $$
create function CalcRouteLengthAsc(bus_pos_lon decimal(20,15), bus_pos_lat decimal(20,15), BusClosestEndPointID int, busStopId int)
returns float
BEGIN
	declare RouteCounter int default BusClosestEndPointID;
	declare BusToStop float;
	declare R1x decimal(20,15);
	declare R1y decimal(20,15);
	declare R2x decimal(20,15);
	declare R2y decimal(20,15);
	declare busStopChosenRouteAscID int;
	select bus_lon from ChosenRouteAsc where id = RouteCounter into R2x;
	select bus_lat from ChosenRouteAsc where id = RouteCounter into R2y;

	set BusToStop = Haversine(R2y, bus_pos_lat, R2x, bus_pos_lon);

	while RouteCounter < busStopId do
		select bus_lon from ChosenRouteAsc where id = RouteCounter into R1x;
		select bus_lat from ChosenRouteAsc where id = RouteCounter into R1y;
		select bus_lon from ChosenRouteAsc where id = RouteCounter+1 into R2x;
		select bus_lat from ChosenRouteAsc where id = RouteCounter+1 into R2y;
		set BusToStop = BusToStop + Haversine(R2y, R1y, R1x, R2x);
		set RouteCounter = RouteCounter+1;
	end while;
	drop temporary table ChosenRouteAsc;
	return BusToStop;
END$$

delimiter $$
drop function if exists CalcRouteLengthDesc $$
create function CalcRouteLengthDesc(bus_pos_lon decimal(20,15), bus_pos_lat decimal(20,15), BusClosestEndPointID int, busStopId int)
returns float
BEGIN
	declare RouteCounter int default BusClosestEndPointID;
	declare BusToStop float;
	declare R1x decimal(20,15);
	declare R1y decimal(20,15);
	declare R2x decimal(20,15);
	declare R2y decimal(20,15);
	select bus_lon from ChosenRouteDesc where id = RouteCounter into R2x;
	select bus_lat from ChosenRouteDesc where id = RouteCounter into R2y;
	set BusToStop = Haversine(R2y, bus_pos_lat, R2x, bus_pos_lon);

	while RouteCounter > busStopId do
		select bus_lon from ChosenRouteDesc where id = RouteCounter into R1x;
		select bus_lat from ChosenRouteDesc where id = RouteCounter into R1y;
		select bus_lon from ChosenRouteDesc where id = RouteCounter-1 into R2x;
		select bus_lat from ChosenRouteDesc where id = RouteCounter-1 into R2y;
		set BusToStop = BusToStop + Haversine(R2y, R1y, R1x, R2x);
		set RouteCounter = RouteCounter-1;
	end while;
	drop temporary table ChosenRouteDesc;
	return BusToStop;
END$$


DELIMITER $$
drop function if exists Haversine $$
create function Haversine(Lat1 decimal(20,15), Lat2 decimal(20,15), Lon1 decimal(20,15), Lon2 decimal(20,15))
returns float
begin
	declare earthR int default 6371;
	declare deltaLat float default Radians(Lat2 - Lat1);
	declare deltaLon float default Radians(Lon2 - Lon1);
	declare lat1Rads float default Radians(Lat1);
	declare lat2Rads float default Radians(Lat2);

	declare a float default (Sin(deltaLat/2) * Sin(deltaLat/2)) + sin(deltaLon/2) * sin(deltaLon/2) * Cos(lat1Rads) * cos(lat2Rads);	
	declare c float default 2 * atan2(sqrt(a), sqrt(1-a));
	declare dist float default earthR * c * 1000;
	return dist;
end $$


DELIMITER $$
drop function if exists CalcBusAvgSpeedAsc $$
create function CalcBusAvgSpeedAsc(BusId int)
returns float
begin
	declare PosCounter int default 1;
	declare MaxPosCounter int;
	declare Distance float default 0;
	declare R1x decimal(20,15);
	declare R1y decimal(20,15);
	declare R2x decimal(20,15);
	declare R2y decimal(20,15);
	declare ThisTime Time;
	declare NextTime Time;
	declare secondsDriven int default 0;
	declare speed float;
	
		drop temporary table if exists BusGPSAsc;
		create TEMPORARY table if not exists BusGPSAsc(
		id int auto_increment primary key, 
		pos_lat decimal(20,15),
		pos_lon decimal(20,15),
		busUpdateTime time
	);
	
	#Get all positional data for this bus
	insert into BusGPSAsc (pos_lat, pos_lon, busUpdateTime)
	select GPSPosition.Latitude,  GPSPosition.Longitude, GPSPosition.Updatetime from GPSPosition
	where GPSPosition.fk_Bus=BusId;
	
	#Get amount of positional data for this bus
	select count(id) from BusGPSAsc into MaxPosCounter;
	#Iterate through positional data calculating how far the bus has traveled
	#and how long it has take it to drive the disntance.
	while PosCounter < MaxPosCounter do
		select pos_lon from BusGPSAsc where id= PosCounter into R1x;
		select pos_lat from BusGPSAsc where id= PosCounter into R1y;
		select pos_lon from BusGPSAsc where id = PosCounter+1 into R2x;
		select pos_lat from BusGPSAsc where id = PosCounter+1 into R2y;
		set Distance = Distance + Haversine(R2y, R1y, R1x, R2x);
		select busUpdateTime from BusGPSAsc where id= PosCounter  into ThisTime;
		select busUpdateTime from BusGPSAsc where id = PosCounter+1  into NextTime;
		set secondsDriven = secondsDriven + (Time_To_Sec(NextTime) - Time_To_Sec(ThisTime));
		set PosCounter = PosCounter + 1;
	end while;
	# Meters / seconds = speed.
	set speed = Distance/secondsDriven;
	drop temporary table BusGPSAsc;
	return speed;
end $$

DELIMITER $$
drop function if exists CalcBusAvgSpeedDesc $$
create function CalcBusAvgSpeedDesc(BusId int)
returns float
begin
	declare PosCounter int default 1;
	declare MaxPosCounter int;
	declare Distance float default 0;
	declare R1x decimal(20,15);
	declare R1y decimal(20,15);
	declare R2x decimal(20,15);
	declare R2y decimal(20,15);
	declare ThisTime Time;
	declare NextTime Time;
	declare secondsDriven int default 0;
	declare speed float;
		drop temporary table if exists BusGPSDesc;
		create TEMPORARY table if not exists BusGPSDesc(
		id int auto_increment primary key, 
		pos_lat decimal(20,15),
		pos_lon decimal(20,15),
		busUpdateTime time
	);
	
	#Get all positional data for this bus
	insert into BusGPSDesc (pos_lat, pos_lon, busUpdateTime)
	select GPSPosition.Latitude,  GPSPosition.Longitude, GPSPosition.Updatetime from GPSPosition
	where GPSPosition.fk_Bus=BusId;
	
	#Get amount of positional data for this bus
	select count(id) from BusGPSDesc into MaxPosCounter;
	#Iterate through positional data calculating how far the bus has traveled
	#and how long it has take it to drive the disntance.
	while PosCounter < MaxPosCounter do
		select pos_lon from BusGPSDesc where id= PosCounter into R1x;
		select pos_lat from BusGPSDesc where id= PosCounter into R1y;
		select pos_lon from BusGPSDesc where id = PosCounter+1 into R2x;
		select pos_lat from BusGPSDesc where id = PosCounter+1 into R2y;
		set Distance = Distance + Haversine(R2y, R1y, R1x, R2x);
		select busUpdateTime from BusGPSDesc where id= PosCounter  into ThisTime;
		select busUpdateTime from BusGPSDesc where id = PosCounter+1  into NextTime;
		set secondsDriven = secondsDriven + (Time_To_Sec(NextTime) - Time_To_Sec(ThisTime));
		set PosCounter = PosCounter + 1;
	end while;
	# Meters / seconds = speed.
	set speed = Distance/secondsDriven;
	drop temporary table BusGPSDesc;
	return speed;
end $$
delimiter ;




