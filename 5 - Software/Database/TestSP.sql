create table BusRoutePoints(
id int AUTO_INCREMENT not null,
bus_lat float not null,
bus_lon float not null,
Primary key (id)
);

create table test(
floatval float);

alter table BusRoutePoints add column IsStop bit default false;


create table BusNumberToRoutePoints(
busNumberId int not null,
busRoutePointId int not null,
Primary key (busNumberId, busRoutePointId),
foreign key (busNumberId) references BusNumbers(busNumberId),
foreign key (busRoutePointId) references BusRoutePoints(id)
);

create table BusNumbers
(
	busNumberId int AUTO_INCREMENT not null,
	busNumber varchar(10) not null,
	primary key(busNumberId)
);

create table Busses
(
	busId int not null,
	fk_busNumberId int not null,
	primary key(busId),
	foreign key (fk_busNumberId) references BusNumbers(busNumberId)
);

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

drop procedure if exists CalcBusToStopTime;
DELIMITER $$
create procedure CalcBusToStopTime(IN BusStopId int, IN routeNumber int, OUT TimeToStopSec int)
BEGIN
	declare RouteId int;
	declare ClosestEndPointId int;
	declare ClosestBusId int;
	declare ClosestBusDistance float;
	declare ClosestBusLat decimal(20,15);
	declare ClosestBusLon decimal(20,15);
	declare ClosestBusSpeed float;
	declare timetostop float;

	select busNumberId from BusNumbers where busNumber = routeNumber into RouteId;
	#Closests bus
	select GetClosestBus(BusStopId, RouteId)  into ClosestBusId;
	#Closets endpoint
	select GetClosestEndpoint(ClosestBusId) into ClosestEndPointId;

	#Get position for closestBus
	select pos_longitude, pos_latitude from GPSPos where BusID = ClosestBusId
	order by posID desc limit 1 into ClosestBusLon, ClosestBusLat;
	
	#Get distance for closests bus;
	select CalcRouteLength(ClosestBusLon, ClosestBusLat, ClosestEndPointId, BusStopId) into ClosestBusDistance;
	
	select CalcBusAvgSpeed(ClosestBusId) into ClosestBusSpeed;
	set timetostop = ClosestBusDistance/ClosestBusSpeed;
	set TimeToStopSec = ClosestBusDistance/ClosestBusSpeed;
	insert into test values (ClosestEndPointId, ClosestBusId, ClosestBusDistance,
						ClosestBusLat, ClosestBusLon, ClosestBusSpeed, timetostop);

	drop table ChosenRoute;
	drop table BussesOnRoute;
	drop table BusGPS;
END$$

delimiter $$
drop function if exists GetClosestBus $$
create function GetClosestBus(busStopId int, routeID int)
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

	#Get all busses on route
	create temporary table BussesOnRoute(
		autoId int auto_increment primary key,
		busId int
	);

	insert into BussesOnRoute (busId) select busId from Busses where fk_busNumberId=routeId;
	select count(busId) from BussesOnRoute into NumberOfBusses;	

	while BusCounter <= NumberOfBusses do
		#Get closests endpoint for those busses
		select busId from BussesOnRoute where autoId = BusCounter into currentBusId;
		select GetClosestEndpoint(currentBusId) into closestsEndPoint; #GetClosestsEndpoint not finished
		#Get distance to bus stop for each bus
		select pos_longitude, pos_latitude from GPSPos where BusID = currentBusId
		order by posID desc limit 1 into busPos_lon, busPos_lat; 

		select CalcRouteLength(busPos_lon, busPos_lat, closestsEndPoint, busStopId) into currentBusDist;
		if (currentBusDist < leastBusDist) then
			set leastBusDist = currentBusDist;
			set closestsBusID = currentBusId;
		end if;
		set BusCounter = BusCounter + 1;
	end while;
	#return bus with closests distance
	return closestsBusID;
end$$

delimiter $$
drop function if exists GetClosestEndpoint $$
create function GetClosestEndpoint(bID int)
returns int
begin
	declare routeId int;
	declare RouteCounter int default 1;
	declare TotalRouteLines int;
	declare BusLastPosLon decimal(20,15);
	declare BusLastPosLat decimal(20,15);
	declare R1x decimal(20,15);
	declare R1y decimal(20,15);
	declare R2x decimal(20,15);
	declare R2y decimal(20,15);
	declare BusDist float ;
	declare PrevBusDist float default 10000000;
	declare ClosestEndPointId int;

	create TEMPORARY table if not exists ChosenRoute(
		id int AUTO_increment primary key,
		bus_lat decimal(20,15),
		bus_lon decimal(20,15)
	);

	select fk_busNumberId from Busses where busId = bID into routeId;

	insert into ChosenRoute (bus_lat,bus_lon)
	select bus_lat,bus_lon from BusRoutePoints
	inner join BusNumberToRoutePoints
	where BusNumberToRoutePoints.busNumberId=routeId
	AND BusNumberToRoutePoints.busRoutePointId=BusRoutePoints.id;

	select count(id) from ChosenRoute into TotalRouteLines;

	while RouteCounter < TotalRoutelines do
		select pos_longitude, pos_latitude from GPSPos where BusID = bID
		order by PosID desc limit 1 into BusLastPosLon, BusLastPosLat;
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
	
	insert into BusGPS (pos_lat, pos_lon, busUpdateTime)
	select pos_latitude, pos_longitude, Updatetime from GPSPos
	where BusID=BusId;

	select count(id) from BusGPS into MaxPosCounter;
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
	set speed = Distance/secondsDriven;
	return speed;
end $$
delimiter ;

drop procedure if exists BusToRoute;
delimiter $$
create procedure BusToRoute()
begin
	declare counter int default 1;
	declare maxcounter int;
	declare lat_pos float;
	declare lon_pos float;

	select count(id) from BusRoutePoints into maxcounter;
	while counter <= maxcounter do
		insert into BusNumberToRoutePoints values (5, counter);
		set counter = counter + 1;
	end while;
end$$
delimiter ;

call BusToRoute()

select GetClosestEndpoint(555);
select CalcRouteLength(9,7,4,6);
update BusNumbers set busNumber = 5 where busNumberId = 5;
call CalcBusToStopTime(50,5,@t1);
select * from test;

truncate table test;
truncate GPSPos;
truncate BusRoutePoints

select * from BusRoutePoints

insert into Busses values (555, 5);


delete from BusRoutePoints where id >= 7;
drop table PointDist;
drop table ChosenRoute;
drop table BussesOnRoute;
select * from GPSPos
update GPSPos set BusID = 101 where posID < 100;
