//This Experiment is for the STEREO

#region License Solar Science (SOL)
/*
 * Solar Science is an add-on by Logan Hardin, also known as Snoopy 20111, for Kerbal Space Program by Squad.
 * "I would prefer to be referred to as Snoopy 20111 in any attributions. Let's be honest, I'm not going to track you down, I would just appreciate it."
 * Praise the Sun
 *
 * Copyright (C) 2015, 2016 Snoopy 20111 (Logan Hardin)
 * Copyright (C) 2019, 2022 zer0Kerbal (zer0Kerbal at hotmail dot com)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */ 
#endregion

using System;
using UnityEngine;
using System.Collections;
using KSP.Localization;

namespace SolarScience
{
    //Inherit ModuleScienceExperiment stuff
    public class SolarExperimentStereo : ModuleScienceExperiment
    {
        public bool debugMode = false;

        //link to a specific image from the AIA
        private readonly string SunImageURL = "https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_211193171.jpg";
        private readonly string sunImageBackup = @"SolarScience/Plugins/Textures/latest_1024_211193171";
        private readonly string blackImage = @"SolarScience/Plugins/Textures/Black";
        private readonly string Open_SFX = @"SolarScience/Plugins/Sounds/Open_STEREO";

        private Texture2D imageOfSun;
        private AudioClip Open_SFX_Sound;
        public FXGroup SoundGroup = null;

        public bool isShowingWindow = false;
        private PopupDialog myPopupDialog;
        private bool internetConnection;

        // Check if you're around the Sun and height from the surface, and if false post the message
        public bool CheckBody()
        {
            if (vessel.mainBody.name == "Sun" && vessel.mainBody.GetAltitude(vessel.CoM) <= 14700000000d)
            {
                if (debugMode)
                    Debug.Log("[Solar Science] Triggered checkBody, returned true");
                return true;
            }
            else
            {
                Debug.Log("[Solar Science] Triggered checkBody, returned false");
                // "This experiment only operates closely around Kerbol (the Sun) !"
                ScreenMessages.PostScreenMessage(Localizer.Format("#SOL-000"), 3, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }
        }

        //Check if the instrument is pointing at least close to the sun
        public bool CheckDirection()
        {
            bool temp;
            //left is up
            if (Vector3.Angle((part.transform.right) *-1, FlightGlobals.currentMainBody.position - vessel.transform.position) <= 10)
            {
                if (debugMode)
                    Debug.Log("[Solar Science] Triggered checkDirection, returned true");
                temp = true;
            }
            else
            {
                if (debugMode)
                    Debug.Log("[Solar Science] Triggered checkDirection, returned false");
                 // "Point it towards Kerbol! You can't take the pictures if you aren't looking at it!"
                ScreenMessages.PostScreenMessage(Localizer.Format("#SOL-001"), 3, ScreenMessageStyle.UPPER_CENTER);
                temp = false;
            }

            if (debugMode)
            {
                Debug.Log("[Solar Science] Angle from forward to body: " + Vector3.Angle(part.transform.forward, FlightGlobals.currentMainBody.position - vessel.transform.position));
                Debug.Log("[Solar Science] Angle from right to body: " + Vector3.Angle(part.transform.right, FlightGlobals.currentMainBody.position - vessel.transform.position));
                Debug.Log("[Solar Science] Angle from up to body: " + Vector3.Angle(part.transform.up, FlightGlobals.currentMainBody.position - vessel.transform.position));
            }

            return temp;
        }

        //Check if the Angular Velocity is less than .05
        public bool CheckAngularVelocity()
        {
            if (vessel.angularVelocity.magnitude <= .05)
            {
                if (debugMode)
                    Debug.Log("[Solar Science] Triggered checkAngularVelocity, returned true");
                return true;
            }
            else
            {
                if (debugMode)
                    Debug.Log("[Solar Science] Triggered checkAngularVelocity, returned false");

                // "Steady your craft! You'll make the pictures blurry!"
                ScreenMessages.PostScreenMessage(Localizer.Format("#SOL-002"), 3, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }
        }

        public void LoadNewImage()
        {

            //Attempt to load image from internet, if there is a connection
            if (internetConnection)
            {
                //If internet available, start loading image from internet
                //Then load in Black.png, which will be replaced from internet
                StartCoroutine(LoadThisFile());
                if (debugMode)
                {
                    Debug.Log("[Solar Science] Image from internet loading now!");
                    Debug.Log("[Solar Science] Current state of internetConnection: " + internetConnection);
                }

                if (GameDatabase.Instance.ExistsTexture(blackImage))
                    imageOfSun = GameDatabase.Instance.GetTexture(blackImage, false);
            }
            //If no connection, find and load in the backup image
            else if (GameDatabase.Instance.ExistsTexture(sunImageBackup))
            {
                imageOfSun = GameDatabase.Instance.GetTexture(sunImageBackup, false);
                if (debugMode)
                {
                    Debug.Log("[Solar Science] Current state of internetConnection: " + internetConnection);
                    Debug.Log("[Solar Science] Backup Image loaded");
                }
            }
            else if (debugMode)
                Debug.Log("[Solar Science] Couldn't find the backup texture");
        }

        private void DrawPopupStuff()
        {
            LoadNewImage();

            //If showing window, image exists, and no popup exists currently
            if (isShowingWindow == true)
            {
                if (myPopupDialog == null && imageOfSun != null)
                {
                    //Release the kraken
                    myPopupDialog = PopupDialog.SpawnPopupDialog(new Vector2(0.3f, 0.3f),
                        new Vector2(0.3f, 0.3f),
                        new MultiOptionDialog(
                            "Solar STEREO Image",
                            "",
                            "Solar STEREO Image",
                            HighLogic.UISkin,
                            new Rect(0.25f, 0.25f, imageOfSun.width / 2, imageOfSun.height / 2),
                            new DialogGUIBase[]
                            {
                                new DialogGUIImage(new Vector2(1, imageOfSun.height/2),
                                    new Vector2(0,0),new Color(1, 1, 1, 1),imageOfSun),

                                new DialogGUIFlexibleSpace(),

                                new DialogGUIHorizontalLayout(
                                    ),

                                new DialogGUIButton(Localizer.Format("#autoLOC_149410"), // "close"
                                    delegate
                                    {
                                        isShowingWindow = false;
                                        imageOfSun = GameDatabase.Instance.GetTexture(blackImage, false);
                                    },
                                    true)
                            }
                        ),
                        false,
                        HighLogic.UISkin);
                }
                myPopupDialog.SetDraggable(true);
            }
            else
            {
                myPopupDialog.Dismiss();
                imageOfSun = GameDatabase.Instance.GetTexture(blackImage, false);
            }
        }

        [Obsolete]
        private IEnumerator LoadThisFile()
        {
            using WWW www_Stereo = new(SunImageURL);
            yield return www_Stereo;
            www_Stereo.LoadImageIntoTexture(imageOfSun);
        }

        [Obsolete]
        private IEnumerator CheckInternetConnection()
        {
            WWW www = new(SunImageURL);
            yield return www;

            if (www.error != null)
            {
                internetConnection = false;
                Debug.Log("[Solar Science] Internet Connection error: " + www.error);
            }
            else
                internetConnection = true;
        }

        public override void OnStart(StartState state)
        {
            StartCoroutine(CheckInternetConnection());
            Open_SFX_Sound = GameDatabase.Instance.GetAudioClip(Open_SFX);
            SoundGroup.audio = gameObject.AddComponent<AudioSource>();
            base.OnStart(state);
        }

        /// <summary>If deploying an Experiment, check the booleans and act accordingly</summary>
        public void DeployExperiment()
        {
            if (CheckBody() && CheckAngularVelocity() && CheckDirection())
            {
                base.DeployExperiment();
                isShowingWindow = true;
                DrawPopupStuff();

                SoundGroup.audio.PlayOneShot(Open_SFX_Sound);
                if (SoundGroup.audio.isPlaying)
                    Debug.Log("[Solar Science] Open_SFX_Sound, Stereo");
                else
                    Debug.Log("[Solar Science] Sound isn't playing, Stereo");
            }
        }

        // If doing an action, check the booleans and act accordingly
        public void DeployAction(KSPActionParam p)
        {
            if (CheckBody() && CheckAngularVelocity() && CheckDirection())
            {
                base.DeployAction(p);
                isShowingWindow = true;
                DrawPopupStuff();

                SoundGroup.audio.PlayOneShot(Open_SFX_Sound);
                if (SoundGroup.audio.isPlaying)
                    Debug.Log("[Solar Science] Open_SFX_Sound, Stereo");
                else
                    Debug.Log("[Solar Science] Sound isn't playing, Stereo");
            }
        }

    }
}
