using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;




namespace LandingHeight
{

    public static class LHFlightData
    {
        public static int lhGUImodeStatic; //1 auto, hidden text, 2 auto green text, 3 red ASL, 4 red AGL
    }

    [KSPScenario(ScenarioCreationOptions.AddToAllGames,GameScenes.FLIGHT)]
    public class LHScenario : ScenarioModule
    {
           

        public override void OnLoad(ConfigNode node)
        {
            try
            {
                LHFlightData.lhGUImodeStatic = int.Parse(node.GetValue("GUIdisplay"));
            }
            catch
            {
                LHFlightData.lhGUImodeStatic = 1;

            }
            if(LHFlightData.lhGUImodeStatic <1 || LHFlightData.lhGUImodeStatic > 4)
            {
                LHFlightData.lhGUImodeStatic = 1;
            }

            //Debug.Log("LH Scen " + LHFlightData.lhGUImodeStatic);
        }
        public override void OnSave(ConfigNode node)
        {
            if(node.HasValue("GUIdisplay"))
            {
                node.RemoveValue("GUIdisplay");
            }
            node.AddValue("GUIdisplay", LHFlightData.lhGUImodeStatic.ToString());
        }
    }


    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LHFlight : MonoBehaviour
    {
        private KSP.UI.Screens.Flight.AltitudeTumbler _tumbler; //stock altitude tumbler object
        public TMPro.TextMeshProUGUI tumblerASLtext; //ASL/AGL text object
        RectTransform lhGUIrect; //supporting component
        GameObject lhGUIgo; //master gameObject for ASL/AGL text
        Button lhBtn; //button for clickage
        FlightGlobals.SpeedDisplayModes spdDisp;

        

        public void Start()
        {
            Debug.Log("Landing Height v2.0 start.");
        }

       

        public void lhButtonClick()
        {
            switch(LHFlightData.lhGUImodeStatic)
            {
                case 1:
                    {
                        LHFlightData.lhGUImodeStatic = 2;
                        SetGUITextMode();
                        break;
                    }
                case 2:
                    {
                        LHFlightData.lhGUImodeStatic = 3;
                        SetGUITextMode();
                        break;
                    }
                case 3:
                    {
                        LHFlightData.lhGUImodeStatic = 4;
                        SetGUITextMode();
                        break;
                    }
                case 4:
                    {
                        LHFlightData.lhGUImodeStatic = 1;
                        SetGUITextMode();
                        break;
                    }
                default:
                    {
                        LHFlightData.lhGUImodeStatic = 1;
                        SetGUITextMode();
                        break;
                    }
            }
        }

        public void LateUpdate() //modify UI in late update or KSP default overrides afaik
        {
            try
            {
                if (_tumbler == null || _tumbler.tumbler == null)
                {
                    _tumbler = UnityEngine.Object.FindObjectOfType<KSP.UI.Screens.Flight.AltitudeTumbler>();

                    lhBtn = _tumbler.gameObject.AddComponent<Button>();
                    lhBtn.onClick.AddListener(() => { lhButtonClick(); });
                    AddText();
                }
                
                if (FlightGlobals.speedDisplayMode == FlightGlobals.SpeedDisplayModes.Surface && LHFlightData.lhGUImodeStatic == 1 || FlightGlobals.speedDisplayMode == FlightGlobals.SpeedDisplayModes.Surface && LHFlightData.lhGUImodeStatic == 2 || LHFlightData.lhGUImodeStatic == 4) //only override if in surface mode
                {
                    _tumbler.tumbler.SetValue(heightToLand());
                }
                if(spdDisp != FlightGlobals.speedDisplayMode) //how we detect mouse click on speed display mode.
                {
                    SetGUITextMode();
                }
            }
            catch
            {
                //no tumbler object found, we hit this on scene change, silently fail
            }
        }

        public void AddText()
        {
            lhGUIgo = new GameObject("LHGUI");
            lhGUIrect = lhGUIgo.AddComponent<RectTransform>();
            lhGUIrect.SetParent(lhGUIgo.transform, false);
            tumblerASLtext = lhGUIgo.AddComponent<TMPro.TextMeshProUGUI>();
            lhGUIgo.transform.SetParent(_tumbler.transform.GetChild(0).gameObject.GetComponent<RectTransform>());
            tumblerASLtext.transform.localPosition = new Vector3(.5f, -.8f, 0); //CRITICAL, set this because defaults block tumbler number rendering

            tumblerASLtext.alignment = TMPro.TextAlignmentOptions.Center;
            tumblerASLtext.fontSize = 16;
            tumblerASLtext.fontStyle = TMPro.FontStyles.Bold;
            tumblerASLtext.font = Resources.Load("Fonts/Arial SDF", typeof(TMPro.TMP_FontAsset)) as TMPro.TMP_FontAsset;

            SetGUITextMode();

        }

        public void SetGUITextMode()
        {
            switch (LHFlightData.lhGUImodeStatic)
            {
                case 1:
                    {
                        tumblerASLtext.text = "";
                        break;
                    }
                case 2:
                    {
                        if (FlightGlobals.speedDisplayMode == FlightGlobals.SpeedDisplayModes.Surface) //only override if in surface mode
                        {
                            tumblerASLtext.text = "AGL";
                        }
                        else
                        {
                            tumblerASLtext.text = "ASL";
                        }

                        tumblerASLtext.color = Color.green;
                        break;
                    }
                case 3:
                    {
                        tumblerASLtext.text = "ASL";
                        tumblerASLtext.color = Color.red;
                        break;
                    }
                case 4:
                    {
                        tumblerASLtext.text = "AGL";
                        tumblerASLtext.color = Color.red;
                        break;
                    }
            }
            spdDisp = FlightGlobals.speedDisplayMode;
                    
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