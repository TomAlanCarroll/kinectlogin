using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectLogin
{
    public class VoiceRecognition
    {
        private Voice[] voices;
        private bool authenticationStatus = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public VoiceRecognition()
        {
            this.voices = new Voice[10];
            //this.voices[0] = new Voice();
            //this.voices[0].addVoiceData("TWO");
        }

        /// <summary>
        /// Gets the voices
        /// </summary>
        /// <returns>An array of voices</returns>
        public Voice[] getVoices()
        {
            return this.voices;
        }

        /// <summary>
        /// Sets the voice at the provided index in voices
        /// </summary>
        public void setVoice(int index, Voice voice)
        {
            try
            {
                voices[index] = voice;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Unable to set voice at index " + index);    
            }
        }

        public bool getAuthenticationStatus()
        {
            return this.authenticationStatus;
        }

        public bool isValid(int v)
        {
            return voices[v].getVoiceData().Count >= 1 && voices[v].getVoiceData().Count <= 8;
        }

        public void record(bool startRecording, int v, EventHandler VoiceDataUpdatedEvent)
        {
            if (startRecording)
            {
                KinectHelper.startVoiceRecording();
            }
            else
            {
                voices[v] = KinectHelper.stopVoiceRecording();
                voices[v].VoiceDataUpdated += VoiceDataUpdatedEvent;
            }
        }

        public bool compare(Voice v1, Voice v2)
        {
            bool match = false;
            Queue<String> s1 = v1.getVoiceData();
            Queue<String> s2 = v2.getVoiceData();
            if (s1.Count == s2.Count)
            {
                for (int i = 0; i < s1.Count; i++)
                {
                    match = s1.ElementAt(i).Equals(s2.ElementAt(i));
                    if (!match) break;
                }
            }
            return match;
        }

        public void Authenticate()
        {
            //record(true, 2, KinectManager.UpdateVoiceData);
            //ExtensionMethods.timer(10);
            //record(false, 2, KinectManager.UpdateVoiceData);
            //authenticationStatus = compare(voices[0], voices[2]);
        }
    }
}
