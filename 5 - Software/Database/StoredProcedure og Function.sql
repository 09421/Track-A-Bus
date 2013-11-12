
drop table test;
create table test(
	EP int,
	bID int,
	dist float,
	lat decimal(20,15),
	lon decimal(20,15),
	speed float,
	timetostop float
);

#CLW TODO: NOT YET COMPLEX!
drop procedure if exists CalcBusToStopTime;
DELIMITER $$
create procedure CalcBusToStopTime(IN stopName varchar(100), IN routeNumber varchar(10), OUT TimeToStopSecAsc int, OUT TimeToStopSecDesc int,
									OUT EndStopNameAsc varchar(100), OUT EndStopNameDesc varchar(100))
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
	declare timetostop float;
	
	#Get BusRoute from routeNumber
	select BusRoute.ID from BusRoute where BusRoute.RouteNumber = routeNumber into RouteId;
	#Get BusStop id from BusStop Name
	select BusStop.ID from BusStop where BusStop.StopName = StopName into StopID;

	#Closests bus, both ways.
	select GetClosestBus(StopId, RouteId,false)  into ClosestBusIdAsc;
	select GetClosestBus(StopId, RouteId,true)  into ClosestBusIdDesc;

	#Get position for closestBus for both ways
	select Latitude, Longtiude from GPSPosition where GPSPosition.fk_Bus = ClosestBusIdAsc
	order by GPSPosition.ID desc limit 1 into ClosestBusLatAsc, ClosestBusLonAsc;
	select Latitude, Longtiude from GPSPosition where GPSPosition.fk_Bus = ClosestBusIdDesc
	order by GPSPosition.ID desc limit 1 into ClosestBusLatDesc, ClosestBusLonDesc;

	#Closets endpoint and distance, both ways.
	select GetClosestEndpoint(ClosestBusId, false) into ClosestEndPointIdAsc;
	select CalcRouteLength(ClosestBusLonAsc, ClosestBusLatAsc, ClosestEndPointIdAsc, BusStopId) into ClosestBusDistanceAsc;
	select GetClosestEndpoint(ClosestBusId, true) into ClosestEndPointIdDesc;
	select CalcRouteLength(ClosestBusLonDesc, ClosestBusLatDesc, ClosestEndPointIdDesc, BusStopId) into ClosestBusDistanceDesc;

	select CalcBusAvgSpeed(ClosestBusIdAsc) into ClosestBusSpeedAsc;
	select CalcBusAvgSpeed(ClosestBusIdDesc) into ClosestBusSpeedDesc;

	set TimeToStopSecAsc = ClosestBusDistanceAsc/ClosestBusSpeedAsc;
	set TimeToStopSecDesc = ClosestBusDistanceDesc/ClosestBusSpeedDesc;

	#Get end stop for route both ways.
	select StopName from BusStop
	join BusRoute_BusStop on BusRoute_BusStop.fk_BusStop=BusStop.ID
	where BusRoute_BusStop.fk_BusRoute = RouteID
	order by busStop.ID desc limit 1 into EndStopNameAsc;

	select StopName from BusStop
	join BusRoute_BusStop on BusRoute_BusStop.fk_BusStop=BusStop.ID
	where BusRoute_BusStop.fk_BusRoute = RouteID
	order by busStop.ID asc limit 1 into EndStopNameDesc;
	
	
	drop table ChosenRoute;
	drop table BussesOnRoute;
	drop table BusGPS;
END$$
delimiter $$
drop function if exists GetClosestBus $$
create function GetClosestBus(stopId int, routeID int, isDescending bit )
returns int
begin
	declare NumberOfBusses int default 0;
	declare BusCounter int default 1;
	declare currentBusId int;
	declare closestsBusID int;
	declare closestsEndPoint int;
	declare busPos_lon decimal(20,15);
	declare busPos_lat decimal(20,15);
	declare currentBusDist float default 0;
	declare leastBusDist float default 1000000000;


	create temporary table BussesOnRoute(
		autoId int auto_increment primary key,
		busId int
	);
	#Get all busses on route that drives  a spefic way
	insert into BussesOnRoute (busId) select Bus.ID from Bus
	where Bus.fk_BusRoute=routeId && Bus.IsDecending=isDecending;

	#Get number of busses on route.
	select count(busId) from BussesOnRoute into NumberOfBusses;	

	#Iterate though busses on that route, finding the closest bus.
	while BusCounter <= NumberOfBusses do
		
		select busId from BussesOnRoute where autoId = BusCounter into currentBusId;
		#Get closests endpoint for those busses
		select GetClosestEndpoint(currentBusId, routeID,isDecending) into closestsEndPoint; 
		#Get latest latitude and longitude of current bus
		select GPSPosition.Latitude, GPSPosition.Longitude from GPSPosition where GPSPosition.fk_Bus=currentBusId
		order by posID desc limit 1 into busPos_lat, busPos_lon; 
		#Calculate distance from bus to stop.
		select CalcRouteLength(busPos_lon, busPos_lat, closestsEndPoint, stopId) into currentBusDist;
		#If the distance from the current bus to the busstop is lesser than the one before.
		#Change closests bus.
		if (currentBusDist < leastBusDist) then
			set leastBusDist = currentBusDist;
			set closestsBusID = currentBusId;
		end if;
		set BusCounter = BusCounter + 1;
	end while;
	drop table BussesOnRoute;
	#return bus with closests distance
	return closestsBusID;
end$$


delimiter $$
drop function if exists GetClosestEndpoint $$
create function GetClosestEndpoint(busID int, routeID int, isDescending bit)
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

	create TEMPORARY table if not exists ChosenRoute(
		id int AUTO_increment primary key,
		bus_lat decimal(20,15),
		bus_lon decimal(20,15)
	);

	#Get route points of chosen route.
	if isDecending = true then
		insert into ChosenRoute (bus_lat,bus_lon)
		select Latitude, Longitude from RoutePoint
		inner join BusRoute_RoutePoint
		where BusRoute_RoutePoint.fk_BusRoute=routeId
		AND BusRoute_RoutePoint.fk_RoutePoint=RoutePoint.ID
		order by(RoutePoint.ID) desc;
	else
		insert into ChosenRoute (bus_lat,bus_lon)
		select Latitude, Longitude from RoutePoint
		inner join BusRoute_RoutePoint
		where BusRoute_RoutePoint.fk_BusRoute=routeId
		AND BusRoute_RoutePoint.fk_RoutePoint=RoutePoint.ID
		order by(RoutePoint.ID) asc;
	end if;
	#Get number of points
	select count(id) from ChosenRoute into TotalRouteLines;
	#Itereate throught endpoints, finding the endpoint closest to the bus
	#Only lesser than, because last point in list can only be endpoint.

	select Latitude, Longitude from GPSPosition where GPSPosition.fk_Bus = busID
	order by GPSPosition.ID desc limit 1 into BusLastPosLat, BusLastPosLon;
	while RouteCounter < TotalRoutelines do
		select bus_lon from ChosenRoute where id = RouteCounter into R1x;
		select bus_lat from ChosenRoute where id = RouteCounter into R1y;
		select bus_lon from ChosenRoute where id = RouteCounter+1 into R2x;
		select bus_lat from ChosenRoute where id = RouteCounter+1 into R2y;
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
drop function if exists CalcRouteLineDist $$
create function CalcRouteLineDist(bus_pos_lon_x decimal(20,15), bus_pos_lat_y decimal(20,15), 
									rx_start decimal(20,15), ry_start decimal(20,15),
									rx_end decimal(20,15), ry_end decimal(20,15)) 
returns float
begin
	declare rA float; 
	declare rB float;
	declare dist float;

	set rA = (ry_end - ry_start) / (rx_end - rx_start);
	set rB = ry_start + (rA*(-rx_start));
	set dist = ABS(bus_pos_lat_y - (rA * bus_pos_lon_x) - rB) / sqrt((rA * rA) + 1);
	return dist;
end$$

delimiter $$
drop function if exists CalcRouteLength $$
create function CalcRouteLength(bus_pos_lon decimal(20,15), bus_pos_lat decimal(20,15), BusClosestEndPointID int, busStopId int)
returns float
BEGIN
	declare RouteCounter int default BusClosestEndPointID;
	declare BusToStop float;
	declare R1x decimal(20,15);
	declare R1y decimal(20,15);
	declare R2x decimal(20,15);
	declare R2y decimal(20,15);
	select bus_lon from ChosenRoute where id = RouteCounter into R2x;
	select bus_lat from ChosenRoute where id = RouteCounter into R2y;
	set BusToStop = Haversine(R2y, bus_pos_lat, R2x, bus_pos_lon);

	while RouteCounter < busStopId do
		select bus_lon from ChosenRoute where id = RouteCounter into R1x;
		select bus_lat from ChosenRoute where id = RouteCounter into R1y;
		select bus_lon from ChosenRoute where id = RouteCounter+1 into R2x;
		select bus_lat from ChosenRoute where id = RouteCounter+1 into R2y;
		set BusToStop = BusToStop + Haversine(R2y, R1y, R1x, R2x);
		set RouteCounter = RouteCounter+1;
	end while;
	drop table ChosenRoute;
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
delimiter ;

drop function if exists CalcBusAvgSpeed;
DELIMITER $$
create function CalcBusAvgSpeed(BusId int)
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
	
		create TEMPORARY table if not exists BusGPS(
		id int auto_increment primary key, 
		pos_lat decimal(20,15),
		pos_lon decimal(20,15),
		busUpdateTime time
	);
	
	#Get all positional data for this bus
	insert into BusGPS (pos_lat, pos_lon, busUpdateTime)
	select Latitude, Longitude, Updatetime from GPSPosition
	where GPSPosition.fk_Bus=BusId;
	
	#Get amount of positional data for this bus
	select count(id) from BusGPS into MaxPosCounter;
	#Iterate through positional data calculating how far the bus has traveled
	#and how long it has take it to drive the disntance.
	while PosCounter < MaxPosCounter do
		select pos_lon from BusGPS where id= PosCounter into R1x;
		select pos_lat from BusGPS where id= PosCounter into R1y;
		select pos_lon from BusGPS where id = PosCounter+1 into R2x;
		select pos_lat from BusGPS where id = PosCounter+1 into R2y;
		set Distance = Distance + Haversine(R2y, R1y, R1x, R2x);
		select busUpdateTime from BusGPS where id= PosCounter  into ThisTime;
		select busUpdateTime from BusGPS where id = PosCounter+1  into NextTime;
		set secondsDriven = secondsDriven + (Time_To_Sec(NextTime) - Time_To_Sec(ThisTime));
		set PosCounter = PosCounter + 1;
	end while;
	# Meters / seconds = speed.
	set speed = Distance/secondsDriven;
	return speed;
end $$
delimiter ;

call CalcBusToStopTime(50,5,@t1);
select * from test;


