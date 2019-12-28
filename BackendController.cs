using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Azure.Cosmos;
using SimbaForceFrontend.Functionality;
using SimbaForceFrontend.Models;
using SimbaForceFrontend.Models.Repository;


namespace SimbaForceFrontend.Controllers
{
    public class BackendController : Controller
    {
        // GET: api
        MlExecRepository mlexecdbrepo = new MlExecRepository(); // use DI later.
        public ActionResult LoginFeature(UserRecord user)
        {
            if (user.userName == "jones" && user.passWord == "onie") 
            { 
                user.validated = true;
                user.lastValidated = DateTime.Now; 
                mlexecdbrepo.Set<UserRecord>(user); 
                return Json(user);
            }
            user.validated = false;
            return Json(user);
        }
        public ActionResult InstrumentStatistics()
        {
            List<Instrument> instruments = Requestor.instruments();
            List<InstrumentPerformance> instrumentPerformances = mlexecdbrepo.Get<InstrumentPerformance>(isntPerf => true).ToList();

            instrumentPerformances = InstrumentStatisticsHelper.SyncInstrumentPerformances(instruments, instrumentPerformances);
            instrumentPerformances = InstrumentStatisticsHelper.UpdateInstrumentPerformances(instrumentPerformances);
            return Json(instrumentPerformances, JsonRequestBehavior.AllowGet);
        }
    }
}