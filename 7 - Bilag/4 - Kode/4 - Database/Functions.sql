
#Calculates the closests RoutePoint for the given bus. Returns the BusRoute_RoutePointID. This works only for the bus driving from first to last stop.
delimiter $$
drop function if exists GetClosestEndpointAsc $$
create function GetClosestEndpointAsc(busID int)
returns int
begin
	declare RouteCounter int default 1;
	declare BusLastPosLon decimal(20,15);
	declare BusLastPosLat decimal(20,15);
	declare R1x decimal(20,15);
	declare R1y decimal(20,15);
	declare R2x decimal(20,15);
	declare R2y decimal(20,15);
	declare BusDist float ;
	#initialize to high number, so first endpoint will be closer,
	declare PrevBusDist float default 100000;
	declare ClosestEndPointId int;
	declare LastChosenID int;
	
	#ChosenRoute holds the data for the route in question.
	drop temporary table if exists ChosenRouteAsc;
	create TEMPORARY table if not exists ChosenRouteAsc(
		id int primary key,
		bus_lat decimal(20,15),
		bus_lon decimal(20,15)
	);

	#Get RoutePoints and the IDs of the Route from the BusRoute_RoutePoint and RoutePointTable.
	#The points are taken in ascending order, meaning from first to last stop.
	insert into ChosenRouteAsc (id,bus_lat,bus_lon)
	select BusRoute_RoutePoint.ID, RoutePoint.Latitude,  RoutePoint.Longitude from RoutePoint
	inner join BusRoute_RoutePoint on BusRoute_RoutePoint.fk_RoutePoint = RoutePoint.ID
	inner join Bus on Bus.fk_BusRoute = BusRoute_RoutePoint.fk_BusRoute
	where Bus.ID = busID
	order by(BusRoute_RoutePoint.ID) asc;

	#Get the last ID, and the first ID;
	select ChosenRouteAsc.ID from ChosenRouteAsc order by id asc limit 1 into RouteCounter;
	select ChosenRouteAsc.ID from ChosenRouteAsc order by id desc limit 1 into LastChosenID;

	#Get last bus position.
	select GPSPosition.Latitude,  GPSPosition.Longitude from GPSPosition where GPSPosition.fk_Bus = busID
	order by GPSPosition.ID desc limit 1 into BusLastPosLat, BusLastPosLon;
	#Itereate throught endpoints, finding the endpoint closest to the bus
	#Only lesser than, because last point in list can only be endpoint. Route counter is added by 1 at end of iteration.
	while RouteCounter < LastChosenID do
		#Get position of the a point and the next one, creating a line.
		select bus_lon from ChosenRouteAsc where id = RouteCounter into R1x;
		select bus_lat from ChosenRouteAsc where id = RouteCounter into R1y;
		select bus_lon from ChosenRouteAsc where id = RouteCounter+1 into R2x;
		select bus_lat from ChosenRouteAsc where id = RouteCounter+1 into R2y;
		#Calculate distance from the line piece generated by two points, and thfe bus.
		set BusDist = CalcRouteLineDist(BusLastPosLon, BusLastPosLat, R1x, R1y, R2x, R2y);
		#Lesser distance, means the bus is closer to that point.
		if BusDist < PrevBusDist then
			set PrevBusDist = BusDist;
			set ClosestEndPointId = RouteCounter+1;
		end if;
		Set RouteCounter = RouteCounter + 1;
	end while;
return ClosestEndPointId;
END$$

#Same as ascending function. Difference is added as comments.
delimiter $$
drop function if exists GetClosestEndpointDesc $$
create function GetClosestEndpointDesc (busID int)
returns int
begin
	declare RouteCounter int;
	declare BusLastPosLon decimal(20,15);
	declare BusLastPosLat decimal(20,15);
	declare R1x decimal(20,15);
	declare R1y decimal(20,15);
	declare R2x decimal(20,15);
	declare R2y decimal(20,15);
	declare BusDist float ;

	declare PrevBusDist float default 100000;
	declare ClosestEndPointId int;
	declare LastChosenID int;


	drop temporary table if exists ChosenRouteDesc;
	create TEMPORARY table if not exists ChosenRouteDesc (
		id int primary key,
		bus_lat decimal(20,15),
		bus_lon decimal(20,15)
	);

	#Chosen route is created the same way, however, the order of the IDs is taken from last to first.
	insert into ChosenRouteDesc  (id,bus_lat,bus_lon)
	select BusRoute_RoutePoint.ID, RoutePoint.Latitude,  RoutePoint.Longitude from RoutePoint
	inner join BusRoute_RoutePoint on BusRoute_RoutePoint.fk_RoutePoint = RoutePoint.ID
	inner join Bus on Bus.fk_BusRoute = BusRoute_RoutePoint.fk_BusRoute
	where Bus.ID = busID
	order by(BusRoute_RoutePoint.ID) desc;

	#The smallest ID is chosen to be the final point and the largest is the starting point.
	#It should iterate the opposite was as ascending.
	select ChosenRouteDesc.ID from ChosenRouteDesc order by id asc limit 1 into LastChosenID;
	select ChosenRouteDesc.ID from ChosenRouteDesc order by id desc limit 1 into RouteCounter;
	
	select GPSPosition.Latitude,  GPSPosition.Longitude from GPSPosition where GPSPosition.fk_Bus = busID
	order by GPSPosition.ID desc limit 1 into BusLastPosLat, BusLastPosLon;

	#RouteCounter (Largest Number) > LastChosenID(Smallests number). RouteCouter subtracts by 1 at end.
	while RouteCounter >  LastChosenID  do
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


#Math function.
#Calculates the distance from the Bus to the line created by the two points.
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

	
	#A linear formula for the line piece is devised as y = rA * x + rB
	#bcx and bcy are the x-coordinate and y-coordinate on the line piece, that is closests to the bus.

	#Line is vertical.
	if rx_end - rx_start = 0 then
		set dist = ABS(bus_pos_lon_x - rx_start);
		set bcx = rx_start;
		set bcy = bus_pos_lat_y;
	#Line is Horizontal.
	elseif ry_end - ry_start = 0 then
		set dist = ABS(bus_pos_lat_y - ry_start);
		set bcx = bus_pos_lon_x;
		set bcy = ry_start;
	#Line is neither horizontal or vertical.
	else
		set rA = (ry_end - ry_start) / (rx_end - rx_start);
		set rB = ry_start + (rA*(-rx_start));
		set dist = ABS(bus_pos_lat_y - (rA * bus_pos_lon_x) - rB) / sqrt((rA * rA) + 1);
		set bcx = ((rA * bus_pos_lat_y) + bus_pos_lon_x - (rA * rB)) / ((rA*rA) + 1);
		set bcy = ((rA*rA*bus_pos_lat_y) + (rA * bus_pos_lon_x) + rB) / ((rA*rA)+ 1);
	end if;
		
	#The linear formula extends the linepiece, generated by the two points, 
	#so a bus that is not close to the linepiece, can be close to the extended linepiece.
	#It is then checked if the closests point for the bus, on the line is inside the rectangle generated by the line piece as a diagonal.
	if (bcx > rx_start AND bcx > rx_end) or (bcx < rx_start AND bcx < rx_end) or
	   (bcy > ry_start AND bcy > ry_end) or (bcy < ry_start AND bcy < ry_end) then
		#if its not, return high number, otherwise return calculated distance.
		return 1000000;
	end if;
	
	return dist;
end$$


#Calculates the distance from the Bus to the closests routePoint of that bus, and then from that RoutePoint to the busstop.
#Acsending and descending are identical with the exception of the table used, and which way it iterates.
delimiter $$
drop function if exists CalcRouteLengthAsc $$
create function CalcRouteLengthAsc(bus_pos_lon decimal(20,15), bus_pos_lat decimal(20,15), BusClosestEndPointID int, busStopId int)
returns float
BEGIN
	#RouteCounter defaults to the closests RoutePoint.
	declare RouteCounter int default BusClosestEndPointID;
	declare BusToStop float;
	declare R1x decimal(20,15);
	declare R1y decimal(20,15);
	declare R2x decimal(20,15);
	declare R2y decimal(20,15);
	declare busStopChosenRouteAscID int;
	
	#Get point for the closest routePoint. ChosenRouteDesc is used for descending bus.
	select bus_lon from ChosenRouteAsc where id = RouteCounter into R2x;
	select bus_lat from ChosenRouteAsc where id = RouteCounter into R2y;
	#Calculate distance from bus to the closests routePoint.
	set BusToStop = Haversine(R2y, bus_pos_lat, R2x, bus_pos_lon);

	#The busstop will have an ID in the BusRoute_RoutePoint table. Iterate until this point is hit.
	#RouteCounter > busStopID for descending bus.
	while RouteCounter < busStopId do
		#Get start and stop points for the line piece.
		select bus_lon from ChosenRouteAsc where id = RouteCounter into R1x;
		select bus_lat from ChosenRouteAsc where id = RouteCounter into R1y;
		select bus_lon from ChosenRouteAsc where id = RouteCounter+1 into R2x;
		select bus_lat from ChosenRouteAsc where id = RouteCounter+1 into R2y;
		#Calculate distance between.
		set BusToStop = BusToStop + Haversine(R2y, R1y, R1x, R2x);	
		#Routecounter-1 for descending bus.
		set RouteCounter = RouteCounter+1;
	end while;
	drop temporary table ChosenRouteAsc;
	return BusToStop;
END$$

#Route length for descending bus. Look at comments for ascending bus.
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



#The haversine formula used to calculate distances in meteers between tow lat/lng points.
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



#Calculates average speed of the bus.
DELIMITER $$
drop function if exists CalcBusAvgSpeed $$
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
	
		drop temporary table if exists BusGPS;
		create TEMPORARY table if not exists BusGPS(
		id int auto_increment primary key, 
		pos_lat decimal(20,15),
		pos_lon decimal(20,15),
		busUpdateTime time
	);
	
	#Get all positional data for this bus
	insert into BusGPS (pos_lat, pos_lon, busUpdateTime)
	select GPSPosition.Latitude,  GPSPosition.Longitude, GPSPosition.Updatetime from GPSPosition
	where GPSPosition.fk_Bus=BusId order by GPSPosition.ID asc;
	
	#Get amount of positional data for this bus
	select count(id) from BusGPS into MaxPosCounter;
	#Iterate through positional data calculating how far the bus has traveled
	#and how long it has take it to drive the disntance.
	while PosCounter < MaxPosCounter do
		#Get current position and next position
		select pos_lon from BusGPS where id= PosCounter into R1x;
		select pos_lat from BusGPS where id= PosCounter into R1y;
		select pos_lon from BusGPS where id = PosCounter+1 into R2x;
		select pos_lat from BusGPS where id = PosCounter+1 into R2y;
		#Calculate distance in meters. This distance will be incremented, so it holds the total distance traveled
		set Distance = Distance + Haversine(R2y, R1y, R1x, R2x);
		#Get update time for those two positions.
		select busUpdateTime from BusGPS where id= PosCounter  into ThisTime;
		select busUpdateTime from BusGPS where id = PosCounter+1  into NextTime;
		#Calculate time it has taken in seconds. This time will incremented, so it holds the total time traveled.
		set secondsDriven = secondsDriven + (Time_To_Sec(NextTime) - Time_To_Sec(ThisTime));
		set PosCounter = PosCounter + 1;
	end while;
	# TotalMeters / TotalSeconds = average speed.
	set speed = Distance/secondsDriven;
	drop temporary table BusGPS;
	return speed;
end $$
