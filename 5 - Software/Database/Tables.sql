drop table if exists BusStop;
create table BusStop
(
	ID int auto_increment primary key,
	StopName varchar(100) not null,
	fk_RoutePoint int not null,
	foreign key (fk_RoutePoint) references RoutePoint(ID)
);
drop table if exists RoutePoint;
create table RoutePoint
(
	ID int auto_increment primary key,
	Latitude decimal(20,15) not null,
	Longitude decimal (20,15) not null
);

drop table if exists BusRoute;
create table BusRoute
(
	ID int auto_increment primary key,
	RouteNumber varchar(10) not null,
	SubRoute int not null
);

drop table if exists BusRoute_BusStop;
create table BusRoute_BusStop
(
	ID int auto_increment primary key,
	fk_BusRoute int not null,
	fk_BusStop int not null,
	foreign key (fk_BusRoute) references BusRoute(ID),
	foreign key (fk_BusStop) references BusStop(ID)
);

drop table if exists BusRoute_BusStop;
create table BusRoute_RoutePoint
(
	ID int auto_increment primary key,
	fk_BusRoute int not null,
	fk_RoutePoint int not null,
	foreign key (fk_BusRoute) references BusRoute(ID),
	foreign key (fk_RoutePoint) references RoutePoint(ID)
);

drop table if exists Bus;
create table Bus
(
	ID int primary key ,
	IsDescedning bool,
	fk_BusRoute int,
	foreign key (fk_BusRoute) references BusRoute(ID)
);

drop table if exists GPSPosition;
create table GPSPosition
(
	ID int auto_increment primary key,
	Latitude decimal(20,15) not null,
	Longitude decimal(20,15) not null,
	fk_Bus int not null,
	foreign key (fk_Bus) references Bus(ID)
);

drop table if exists Waypoint;
CREATE TABLE Waypoint (
  ID int(11) AUTO_INCREMENT primary key,
  Latitude decimal(20,15) NOT NULL,
  Longitude decimal(20,15) NOT NULL,
  fk_BusRoute int NOT NULL references BusRoute(ID)
);
