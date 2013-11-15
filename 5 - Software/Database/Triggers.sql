drop trigger if exists BusStopToRouteTrigger;
delimiter &&
create trigger BusStopToRouteTrigger before insert on BusRoute_BusStop
for each row 
begin
	set New.IsDescending = true;
end &&
