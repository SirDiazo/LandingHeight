using System;
using System.Collections.Generic;
using UnityEngine;



namespace LandingHeight
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LHFlight : MonoBehaviour
    {
        
        public void LateUpdate() //modify UI in late update or KSP default overrides
        {

            if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Surface) //only override if in surface mode
            {
                FlightUIController UI = FlightUIController.fetch;
                UI.alt.setValue(Math.Round(heightToLand(),0));
            }
           
        }

        public double heightToLand() //leave public so other mods can call
        {
            double landHeight = 0;
            bool firstRay = true;
            if (FlightGlobals.ActiveVessel.LandedOrSplashed) //if landed or splashed, height is 0
            {
                landHeight = 0;
            }
            else if (FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude > 2400) //raycast goes wierd outside physics range
            {
                landHeight = FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude; //more then 2400 above ground, just use vessel CoM
            }
            else //inside physics range, goto raycast
            {
                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    if (p.collider.enabled) //only check part if it has collider enabled
                    {
                        Vector3 partEdge = p.collider.ClosestPointOnBounds(FlightGlobals.currentMainBody.position); //find collider edge closest to ground
                        RaycastHit pHit;
                        Ray pRayDown = new Ray(partEdge, FlightGlobals.currentMainBody.position);
                        LayerMask pRayMask = 33792; //layermask does not ignore layer 0, why?
                        if (Physics.Raycast(pRayDown, out pHit,Mathf.Infinity ,pRayMask)) //cast ray
                        {

                            if (firstRay)
                            {

                                
                                        landHeight = pHit.distance;
                                
                                firstRay = false;
                            }
                            else
                            {
                                
                                        landHeight = Math.Min(landHeight, pHit.distance);
                                    
                              
                            }
                            //if (pHit.transform.gameObject.layer != 10 && pHit.transform.gameObject.layer != 15)  //Error trap, ray should only hit layers 10 and 15
                            //{
                            //    print(p.name + " " + pHit.transform.gameObject.layer + " " + pHit.collider.name + " " + pHit.distance);
                            //}

                        }
                        else if (!firstRay) //error trap, ray hit nothing
                        {
                            landHeight = FlightGlobals.ActiveVessel.altitude;
                            firstRay = false;
                        }
                    }

                }
            }

            if (FlightGlobals.ActiveVessel.mainBody.bodyName == "Kerbin" || FlightGlobals.ActiveVessel.mainBody.bodyName == "Laythe")
            {
                if (landHeight > FlightGlobals.ActiveVessel.altitude)
                {
                    landHeight = FlightGlobals.ActiveVessel.altitude;
                }
            }
            
            return landHeight;
        }
    }
}
