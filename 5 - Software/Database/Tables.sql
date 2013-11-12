create table BusStop
(
	ID int auto_increment,
	StopName varchar(100),
	fk_RoutePoint int,
	primary key (ID),
	foreign key (fk_RoutePoint) references RoutePoint(ID)
);

create table RoutePoint
(
	ID int auto_increment primary key,
	Latitude decimal(20,15),
	Longitude decimal (20,15)
);

create table BusRoute
(
	ID int auto_increment primary key,
	RouteNumber varchar(10),
	SubRoute int
);

create table BusRoute_BusStop
(
	fk_BusRoute int,
	fk_BusStop int,
	foreign key (fk_BusRoute) references BusRoute(ID),
	foreign key (fk_BusStop) references BusStop(ID)
);

create table BusRoute_RoutePoint
(
	fk_BusRoute int,
	fk_RoutePoint int,
	foreign key (fk_BusRoute) references BusRoute(ID),
	foreign key (fk_RoutePoint) references RoutePoint(ID)
);

create table Bus
(
	ID int primary key,
	fk_BusRoute int,
	foreign key (fk_BusRoute) references BusRoute(ID)
);

create table GPSPosition
(
	ID int auto_increment primary key,
	Latitude decimal(20,15),
	Longitude decimal(20,15),
	fk_Bus int,
	foreign key (fk_Bus) references Bus(ID)
);