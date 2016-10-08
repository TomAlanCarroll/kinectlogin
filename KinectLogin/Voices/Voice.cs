using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectLogin
{
    [Serializable()]
    public class Voice
    {
        private int passwordTokens = 4;

        public Voice()
        {
            if (KinectManager.getVoicePassword() != null && KinectManager.getVoicePassword().getVoiceData() != null)
            {
                this.passwordTokens = KinectManager.getVoicePassword().getVoiceData().Count;
            }
        }

        public int getPasswordTokens()
        {
            return this.passwordTokens;
        }

        public void setPasswordTokens(int passwordTokens)
        {
            this.passwordTokens = passwordTokens;
        }

        private Queue<String> voiceData = new Queue<String>();

        public event EventHandler VoiceDataUpdated;

        public void addVoiceData(String s)
        {
            if (voiceData.Count >= passwordTokens)
            {
                voiceData.Dequeue();
                voiceData.Enqueue(s);
            }
            else
            {
                voiceData.Enqueue(s);
            }

            if (this.VoiceDataUpdated != null)
            {
                this.VoiceDataUpdated(this, new EventArgs());
            }
        }

        public Queue<String> getVoiceData()
        {
            return ExtensionMethods.DeepClone(this.voiceData);
        }

        public override string ToString()
        {
            if(voiceData != null)
            {
                string tokenizedString = "";
                int i;

                for(i = 0; i < voiceData.Count; i++)
                {
                    tokenizedString += voiceData.ElementAt(i);

                    if(i < voiceData.Count - 1)
                    {
                        tokenizedString += ", ";
                    }
                }

                return tokenizedString;
            }
            else
            {
                return "";
            }
        }
    }
}
