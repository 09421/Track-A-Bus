

drop trigger if exists BusStopToRouteTrigger;
delimiter &&
create trigger BusStopToRouteTrigger after insert on BusRoute_BusStop
for each row
begin
	
	declare BusStopID int;
	declare BusRouteID int;
	declare RouteCount int;
	declare Counter int default 0;
	declare CurrentPointID int;
	declare ClosestEndPointID int;
	declare Closest2EndPointID int;
	declare totalRoutePoints int;
	declare totalRoutePointsNoStops int;
	declare routePointsStopInc int;

	create temporary table if not exists BusRoute_RoutePoint_temp(
		autoId int primary key auto_increment,
		br int,
		rp int
	);
	
	create temporary table if not exists Split1(
		sAutoId int primary key auto_increment,
		s1br int,
		s1rp int
	);
	
	select RoutePoint.ID from RoutePoint
	join BusStop on BusStop.fk_RoutePoint = RoutePoint.ID
	where BusStop.ID = new.fk_BusStop
	into CurrentPointID;

	set BusStopID = new.fk_BusStop;
	set BusRouteID = new.fk_BusRoute;

	insert into BusRoute_RoutePoint_temp (br, rp)
	select BusRoute_RoutePoint.fk_BusRoute, BusRoute_RoutePoint.fk_RoutePoint from BusRoute_RoutePoint 
	where BusRoute_RoutePoint.fk_BusRoute=BusRouteID;

	select count(BusRoute_RoutePoint_temp.autoId) from BusRoute_RoutePoint_temp into totalRoutePoints;

     select count(BusRoute_RoutePoint_temp.autoId) from BusRoute_RoutePoint_temp
	 where BusRoute_RoutePoint_temp.rp not in (select BusStop.fk_RoutePoint from BusStop)
     into totalRoutePointsNoStops;

	set routePointsStopInc = totalRoutePoints - totalRoutePointsNoStops;

	select GetEndPoints_BusStop(BusRouteID, BusStopID) into ClosestEndPointID;
	
	insert into Split1 select * from BusRoute_RoutePoint_temp where BusRoute_RoutePoint_temp.autoId <= ClosestEndPointID + routePointsStopInc;
	insert into Split1 values (ClosestEndPointID+routePointsStopInc+1,BusRouteID, CurrentPointID);
	insert into Split1 select autoId+routePointsStopInc+1,br,rp from BusRoute_RoutePoint_temp where BusRoute_RoutePoint_temp.autoId > ClosestEndPointID + routePointsStopInc;

	delete from BusRoute_RoutePoint where BusRoute_RoutePoint.fk_BusRoute = BusRouteID;
	insert into BusRoute_RoutePoint select * from Split1;
	drop temporary table StopChosenRoute;
	drop temporary table BusRoute_RoutePoint_temp;
	drop temporary table Split1;

end &&
delimiter ;
drop function if exists GetEndPoints_BusStop;
delimiter &&
create function GetEndPoints_BusStop(busRouteID int, BusStopID int)
returns int
begin
		declare MaxCounter int;
		declare Counter int default 1;
		declare epID int;
		declare ep2ID int;
		declare EP2Next float;
		declare StopDist float ;
		declare StopEPDist float;
		declare PrevStopDist float default 10000000;
		declare EP2NextNull int;
		declare busStopLat decimal(20,15);
		declare busStopLon decimal(20,15);
		declare R1x decimal(20,15);
		declare R1y decimal(20,15);
		declare R2x decimal(20,15);
		declare R2y decimal(20,15);

		create TEMPORARY table if not exists StopChosenRoute(
		crID int auto_increment primary key,
		bus_lat decimal(20,15),
		bus_lon decimal(20,15));

		insert into StopChosenRoute (bus_lat,bus_lon)
		select Latitude, Longitude from RoutePoint
		inner join BusRoute_RoutePoint on RoutePoint.ID = BusRoute_RoutePoint.fk_RoutePoint
		where BusRoute_RoutePoint.fk_BusRoute=busRouteID
		and RoutePoint.ID not in (select BusStop.fk_RoutePoint from BusStop);

		select count(StopChosenRoute.crID) from StopChosenRoute into MaxCounter;	

		select Latitude, Longitude from RoutePoint
		join BusStop on BusStop.fk_RoutePoint = RoutePoint.ID
		where BusStop.ID = BusStopID
		into busStopLat, busStopLon;

		while Counter < MaxCounter do
		select bus_lon from StopChosenRoute where StopChosenRoute.crID = Counter into R1x;
		select bus_lat from StopChosenRoute where StopChosenRoute.crID = Counter into R1y;
		select bus_lon from StopChosenRoute where StopChosenRoute.crID = Counter+1 into R2x;
		select bus_lat from StopChosenRoute where StopChosenRoute.crID = Counter+1 into R2y;
		set StopDist = CalcRouteLineDist(busStopLon, busStopLat, R1x, R1y, R2x, R2y);
		set StopEPDist = Haversine(busStopLat, R1y, busStopLon, R1x);
		
		if StopDist < PrevStopDist and StopEPDist < 100 then
			set PrevStopDist = StopDist;
				set epID = Counter+1;
				set ep2ID = Counter;
		end if;
		Set Counter = Counter + 1;
	end while;	
	delete from StopChosenRoute where crID >= 1;
	#set closestEndPointID = epID;
	return ep2ID;

end&&



 