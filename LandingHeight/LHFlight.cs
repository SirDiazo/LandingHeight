using System;
using System.Collections.Generic;
using UnityEngine;



namespace LandingHeight
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LHFlight : MonoBehaviour
    {
        
        public void LateUpdate() //modify UI in late update or KSP default overrides afaik
        {

            if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Surface) //only override if in surface mode
            {
                FlightUIController UI = FlightUIController.fetch;
                UI.alt.setValue(heightToLand());
            }
           
        }

        public class partDist //part's distace to CelestialBody CoM for distance sort
        {
            public Part prt;
            public float dist;
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
                List<Part> partToRay = new List<Part>(); //list of parts to ray
                if (FlightGlobals.ActiveVessel.Parts.Count < 50) //if less then 50 parts, just raycast all parts
                {
                    partToRay = FlightGlobals.ActiveVessel.Parts;
                }
                else //if over 50 parts, only raycast the 30 parts closest to ground. difference between 30 and 50 parts to make the sort worth the processing cost, no point in running the sort on 31 parts as the sort costs more processor power then 1 raycast
                {
                    List<partDist> partHeights = new List<partDist>(); //only used above 50 parts, links part to distance to ground
                    foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                    {
                        partHeights.Add(new partDist() { prt = p, dist = Vector3.Distance(p.transform.position, FlightGlobals.ActiveVessel.mainBody.position) }); //create list of parts and their distance to ground
                        //print("a: " + Vector3.Distance(p.transform.position, FlightGlobals.ActiveVessel.mainBody.position));
                    }
                    partHeights.Sort((i,j) => i.dist.CompareTo(j.dist)); //sort parts so parts closest to ground are at top of list
                    for (int i = 0; i < 30; i = i + 1)
                    {
                        partToRay.Add(partHeights[i].prt); //make list of 30 parts closest to ground
                        //print("b: " + i + " " + partHeights[i].prt.name + " " + partHeights[i].dist + " " + Vector3.Distance(FlightGlobals.ActiveVessel.CoM, FlightGlobals.ActiveVessel.mainBody.position));
                    }

                }

                foreach (Part p in partToRay)
                {
                    if (p.collider.enabled) //only check part if it has collider enabled
                    {
                        Vector3 partEdge = p.collider.ClosestPointOnBounds(FlightGlobals.currentMainBody.position); //find collider edge closest to ground
                        RaycastHit pHit;
                        Ray pRayDown = new Ray(partEdge, FlightGlobals.currentMainBody.position);
                        LayerMask pRayMask = 33792; //layermask does not ignore layer 0, why?
                        if (Physics.Raycast(pRayDown, out pHit,Mathf.Infinity ,pRayMask)) //cast ray
                        {

                            if (firstRay) //first ray this update, always set height to this
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
                if (landHeight < 1) //if we are in the air, always display an altitude of at least 1
                {
                    landHeight = 1;
                }
            }

            if (FlightGlobals.ActiveVessel.mainBody.ocean) //if mainbody has ocean we land on water before the seabed
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
