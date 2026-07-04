//This Experiment is for the KMI

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
    /// <summary>Inherit ModuleScienceExperiment stuff</summary>
    public class SolarExperimentKmi : ModuleScienceExperiment
    {
        public bool debugMode = false;

        //link to a specific image from the AIA
        private readonly string SunImageURL = "https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_HMIB.jpg";
        private readonly string sunImageBackup = @"SolarScience/Plugins/Textures/latest_1024_HMIB";
        public readonly string blackImageURL = @"SolarScience/Plugins/Textures/Black";
        public readonly string Open_SFX = @"SolarScience/Plugins/Sounds/Open_KMI";

        private Texture2D imageOfSun;
        private readonly Texture2D blackImage;
        private AudioClip Open_SFX_Sound;
        public FXGroup SoundGroup = null;

        public bool isShowingWindow = false;
        private PopupDialog myPopupDialog;
        private bool internetConnection;


        /// <summary>Check if you're around the Sun and height from the surface, and if false post the message</summary>
        /// <returns></returns>
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

        /// <summary>Check if the instrument is pointing at least close to the sun</summary>
        /// <returns></returns>
        public bool CheckDirection()
        {
            bool temp;

            if (Vector3.Angle(part.transform.forward, FlightGlobals.currentMainBody.position - vessel.transform.position) <= 10)
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
                Debug.Log("[Solar Science] Angle from part to body: " + Vector3.Angle(part.transform.forward, FlightGlobals.currentMainBody.position - vessel.transform.position));
            return temp;
        }

        /// <summary>Check if the Angular Velocity is less than .05</summary>
        /// <returns></returns>
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

        /// <summary>Attempt to load image from internet, if there is a connection</summary>
        public void LoadNewImage()
        {
            
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

                if (GameDatabase.Instance.ExistsTexture(blackImageURL))
                    imageOfSun = GameDatabase.Instance.GetTexture(blackImageURL, false);
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
                            "Solar KMI Image",
                            "",
                            "Solar KMI Image",
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
                                        imageOfSun = GameDatabase.Instance.GetTexture(blackImageURL, false);
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
                imageOfSun = GameDatabase.Instance.GetTexture(blackImageURL, false);
            }
        }

        [Obsolete]
        private IEnumerator LoadThisFile()
        {
            using WWW www = new(SunImageURL);
            yield return www;
            www.LoadImageIntoTexture(imageOfSun);
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

        /// <summary>On Start</summary>
        /// <param name="state"></param>
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
                    Debug.Log("[Solar Science] Open_SFX_Sound, KMI");
                else
                    Debug.Log("[Solar Science] Sound isn't playing, KMI");
            }
        }

        /// <summary>If doing an action, check the booleans and act accordingly</summary>
        /// <param name="p"></param>
        public void DeployAction(KSPActionParam p)
        {
            if (CheckBody() && CheckAngularVelocity() && CheckDirection())
            {
                base.DeployAction(p);
                isShowingWindow = true;
                DrawPopupStuff();

                SoundGroup.audio.PlayOneShot(Open_SFX_Sound);
                if (SoundGroup.audio.isPlaying)
                    Debug.Log("[Solar Science] Open_SFX_Sound, KMI");
                else
                    Debug.Log("[Solar Science] Sound isn't playing, KMI");
            }
        }
    }
}
