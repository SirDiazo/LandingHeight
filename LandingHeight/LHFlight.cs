using System;
using System.Collections.Generic;
using UnityEngine;




namespace LandingHeight
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LHFlight : MonoBehaviour
    {
        private KSP.UI.Screens.Flight.AltitudeTumbler _tumbler;
        public void Start()
        {
            Debug.Log("Landing Height v1.9 start.");
        }

        public void LateUpdate() //modify UI in late update or KSP default overrides afaik
        {

            // print(FlightUIController.speedDisplayMode);
            try
            {
                if (_tumbler == null || _tumbler.tumbler == null)
                {
                    _tumbler = UnityEngine.Object.FindObjectOfType<KSP.UI.Screens.Flight.AltitudeTumbler>();
                }
                if (FlightGlobals.speedDisplayMode == FlightGlobals.SpeedDisplayModes.Surface) //only override if in surface mode
                {
                    //UnityEngine.Object[] tumblers = UnityEngine.Object.FindObjectsOfType<KSP.UI.Screens.Flight.AltitudeTumbler>();
                    //Debug.Log("cnt " + tumblers.Length);
                    //FlightUIController.speedDisplayMode.
                    //FlightUIController UI = FlightUIController.fetch;
                    //UI.alt.setValue(heightToLand());
                    _tumbler.tumbler.SetValue(heightToLand());
                    //Debug.Log(FlightUIController.fetch.alt.
                    //FlightUIController.fetch.alt.setValue(heightToLand());
                }
            }
            catch
            {
                //Debug.Log("LH " + e);
                //no tumbler object found, we hit this on scene change, silently fail
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
            double landHeightBackup = 0;
            bool firstRay = true;



            if (FlightGlobals.ActiveVessel.LandedOrSplashed) //if landed or splashed, height is 0
            {
                landHeight = 0;
                landHeightBackup = 0;
                //Debug.Log("LH-A");
            }
            else if (FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude > 2400) //raycast goes wierd outside physics range
            {
                landHeight = FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude; //more then 2400 above ground, just use vessel CoM
                landHeightBackup = FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude; //more then 2400 above ground, just use vessel CoM
                //Debug.Log("LH-B");
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
                    partHeights.Sort((i, j) => i.dist.CompareTo(j.dist)); //sort parts so parts closest to ground are at top of list
                    for (int i = 0; i < 30; i = i + 1)
                    {
                        partToRay.Add(partHeights[i].prt); //make list of 30 parts closest to ground
                        //print("b: " + i + " " + partHeights[i].prt.name + " " + partHeights[i].dist + " " + Vector3.Distance(FlightGlobals.ActiveVessel.CoM, FlightGlobals.ActiveVessel.mainBody.position));
                    }

                }

                ////test code START
                //GameObject go = pHit.collider.gameObject;
                //Debug.Log("LH Hit GO " + go.layer + "|" + go.name + "|" + go.tag +"|"+go.GetType()+"|"+pHit.distance);
                //if(go.GetComponent<PQ>() != null)
                //{
                //    Debug.Log("LH Hit PQS!");
                //}
                //else
                //{
                //    Debug.Log("LH Hit something else");
                //}
                //foreach (Component co in go.GetComponents<UnityEngine.Object>())
                //{
                //    Debug.Log("LH Co in Go hit " + co.name + "|" + co.tag + "|" + co.GetType());
                //}
                //foreach (Part cop in go.GetComponentsInParent<Part>())
                //{
                //    Debug.Log("LH  part hit " + FlightGlobals.ActiveVessel.Parts.Contains(cop));
                //}

                //test code END
                //Debug.Log("LH Alt " + FlightGlobals.ActiveVessel.pqsAltitude + "|" + FlightGlobals.ActiveVessel.altitude);
                //Debug.Log("LH START!");
               // Debug.Log("LH distances" + FlightGlobals.ActiveVessel.mainBody.Radius + "|" + FlightGlobals.ActiveVessel.PQSAltitude());
                foreach (Part p in partToRay)
                {
                    try
                    {
                        if (p.collider.enabled) //only check part if it has collider enabled
                        {
                            
                            Vector3 partEdge = p.collider.ClosestPointOnBounds(FlightGlobals.currentMainBody.position); //find collider edge closest to ground
                            RaycastHit pHit;
                            Vector3 rayDir = (FlightGlobals.currentMainBody.position - partEdge).normalized;
                            Ray pRayDown = new Ray(partEdge, rayDir);
                            LayerMask testMask = 32769; //hit layer 0 for parts and layer 15 for groud/buildings
                           // Debug.Log("LH test mask " + testMask.ToString());
                            //LayerMask testMask = 32769;
                            //if (Physics.Raycast(pRayDown, out pHit, (float)(FlightGlobals.ActiveVessel.mainBody.Radius + FlightGlobals.ActiveVessel.altitude), pRayMask)) //cast ray
                            if (Physics.Raycast(pRayDown, out pHit, (float)(FlightGlobals.ActiveVessel.mainBody.Radius + FlightGlobals.ActiveVessel.altitude), testMask)) //cast ray
                            {
                               // Debug.Log("LH hit dist orig " + pHit.distance);
                                if (pHit.collider.gameObject.layer == 15 || pHit.collider.gameObject.layer == 0 && !FlightGlobals.ActiveVessel.parts.Contains(pHit.collider.gameObject.GetComponentInParent<Part>()))
                                {//valid hit, accept all layer 15 hits but only those on layer 0 when part hit is not part of ActiveVessel
                                    float hitDist = pHit.distance; //need to do math so make a local variable for it

                                    if(FlightGlobals.ActiveVessel.mainBody.ocean)
                                    {
                                        if(FlightGlobals.ActiveVessel.PQSAltitude() < 0) //if negative, over ocean and pqs layer is seabed
                                        {
                                            hitDist = hitDist + (float)FlightGlobals.ActiveVessel.PQSAltitude(); //reduce distance of raycast by distance from seabed to sea level, remebed PQSAltitude is negative so 'add' it to reduce the distance
                                            //Debug.Log("LH hit dist after ocean" + hitDist);
                                        }
                                    }
                               
                                    if (firstRay) //first ray this update, always set height to this
                                    {


                                        landHeight = hitDist;

                                        firstRay = false;
                                        //Debug.Log("LH 3 " + landHeight);

                                    }
                                    else
                                    {

                                        landHeight = Math.Min(landHeight, hitDist);
                                        //Debug.Log("LH 3a " + landHeight + "|" + pHit.distance);

                                    }
                                }

                            }
                            else if (!firstRay) //error trap, ray hit nothing
                            {
                                landHeight = Math.Min(landHeight, FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude);
                                firstRay = false;
                            }
                            //Debug.Log("LH hit dist orig " + pHit.distance);
                        }
                    }
                    catch
                    {
                        landHeight = FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude;
                        firstRay = false;
                        //Debug.Log("LH 2");
                    }

                }
                //if(landHeight == 0)
                //{
                //    landHeight = landHeightBackup;
                //}
                if (!firstRay && landHeight == 0)
                {
                    landHeight = FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude;
                    firstRay = false;
                }
                if (landHeight < 1) //if we are in the air, always display an altitude of at least 1
                {
                    //Debug.Log("LH 4 " + landHeight);
                    landHeight = 1;
                   //Debug.Log("LH 4");
                }
                //Debug.Log("LH 5 " + landHeight);
            }

            //if (FlightGlobals.ActiveVessel.mainBody.ocean) //if mainbody has ocean we land on water before the seabed
            //{
            //    //Debug.Log("LH 5");
            //    if (landHeight > FlightGlobals.ActiveVessel.altitude)
            //    {
            //        landHeight = FlightGlobals.ActiveVessel.altitude;
            //        //Debug.Log("LH 6");
            //    }
            //}

            return landHeight;
        }
    }
}