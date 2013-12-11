using System;
namespace MapDrawRouteTool.Controllers
{
    interface IStopController
    {
        int Delete(string stop);
        System.Web.Mvc.JsonResult GetAllStops();
        System.Web.Mvc.JsonResult GetPosistion(string stopName);
        int SaveChangeToStop(string oldName, string newName, string NewPos);
        int Save(string c, string n);
    }
}
