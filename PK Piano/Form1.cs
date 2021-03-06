﻿using System;
using System.Windows.Forms;
using System.Media;
using System.Text;

namespace PK_Piano
{
    public partial class Form1 : Form
    {
        //I created this program to make the EarthBound Music Editor easier for myself to use.
        //It mostly copies hex values into the clipboard, allowing me to paste them in.
        //Some calculation has to take place for the more complicated commands like the echo buffer.
        //It also calculates the hex values for note lengths, which have to be manually specified.
        //The first version of this program was written in 2012. I was taking a VB course in my first semester of college, and made this as a fun side project.
        //As a result, hoever, the code isn't laid out how it'd be if I made something like this from scratch today.
        //For more information on the music engine itself, see https://wiki.superfamicom.org/snes/show/Nintendo+Music+Format+%28N-SPC%29

        public Form1()
        {
            echoDelay = 0x00;
            InitializeComponent();
        }

        bool sfxEnabled = false;
        byte octave = 4; //used in the note buttons' if statements
        byte lastNote;
        string transposeValue = "00"; //this is what will be copied to the clipboard for channel transpose

        byte noteLength = 0x18;
        int multiplier = 1;
        byte noteStacatto = 0x7; //The N-SPC note length format is [length], [staccato and volume], which might look like [18 7F]
        byte noteVolume = 0xF;

        byte echoChannels;
        byte echoVolume; //Change these defaut values to whatever the trackBars' initial values end up to be...
        byte echoDelay;
        byte echoFeedback;
        byte echoFilter;
        

        private void SendNote(byte input)
        {
            //Takes a byte, puts it in the label, and puts it in the clipboard
            FormatNoteLength();
            var note = "[" + input.ToString("X2") + "]";
            DispLabel.Text = note;
            Clipboard.SetText(note);

            UpdateChannelTranspose(input);
        }

        private void SendNote(string input)
        {
            //An alternate version for manually setting the string
            //Probably only needed for the "XX" ones, which should hopefully never happen
            FormatNoteLength();
            DispLabel.Text = $"[{input}]";
            Clipboard.SetText(input);
        }

        private void UpdateChannelTranspose(byte input)
        {
            if (lastNote != 0)
            {
                transposeValue = (input - lastNote).ToString("X2");

                if (transposeValue.Length == 8)
                    transposeValue = transposeValue.Substring(6, 2);

                btnChannelTranspose.Text = $"Transpose (last one was [{transposeValue}])"; //set the button text
            }

            lastNote = input;
        }
        
        private string FormatNoteLength()
        {
            var output = $"[{(noteLength * multiplier):X2} {noteStacatto:X}{noteVolume:X}]";
            Clipboard.SetText(output);

            //show the divide button if the length is bigger than the maximum length a note can have
            btnDividePrompt.Visible = EBM_Note_Data.lengthIsInvalid(noteLength * multiplier);

            LengthDisplay.Text = output;
            return output;
        }

        private void btnDividePrompt_Click(object sender, EventArgs e)
        {
            var lengthResult = EBM_Note_Data.validateNoteLength(noteLength * multiplier);
            if (lengthResult[1] == 1) return; //only proceed if division is necessary

            var message = $"Instead of that huge value, use {getWrittenNumber(lengthResult[1])} "
                        + $"notes with this value instead: [{lengthResult[0]:X2}]";

            MessageBox.Show(message, "Divided note length");
        }

        private string getWrittenNumber(int input)
        {
            string writtenNumber;
            switch (input) //this will only be used in note length multipliers, so only these three numbers will ever be needed
            {
                case 2:
                    writtenNumber = "two";
                    break;
                case 3:
                    writtenNumber = "three";
                    break;
                case 4:
                    writtenNumber = "four";
                    break;
                default:
                    writtenNumber = "ERROR";
                    break;
            }
            return writtenNumber;
        }

        private void cboNoteLength_TextChanged(object sender, EventArgs e)
        {
            //data validation
            try
            {
                //this might be useful in future projects: http://stackoverflow.com/questions/13158969/from-string-textbox-to-hex-0x-byte-c-sharp
                var userInput = int.Parse(cboNoteLength.Text, System.Globalization.NumberStyles.HexNumber);
                noteLength = (byte)userInput;
            }
            catch
            {
                cboNoteLength.Text = noteLength.ToString("X2");
            }

            FormatNoteLength(); //update other parts of the program that use length
        }

        private void txtMultiplier_TextChanged(object sender, EventArgs e)
        {
            multiplier = (int)txtMultiplier.Value;
            LengthDisplay.Text = FormatNoteLength();
            if (sfxEnabled) new SoundPlayer(Properties.Resources.ExtraAudio_UpDown_Tick).Play();
        }

        private void SetAllEchoValues()
        {
            echoVolume = (byte)trackBarEchoVol.Value;
            echoDelay = (byte)trackBarEchoDelay.Value;
            echoFeedback = (byte)trackBarEchoFeedback.Value;
            echoFilter = (byte)trackBarEchoFilter.Value;
        }

        private void CalculateEchoChannelCode()
        {
            SetAllEchoValues(); //I keep getting 00s until I move one of the sliders, which is annoying. Hopefully this should fix it.

            var scratchPaper = ""; //Build up the binary number bit by bit

            scratchPaper += getBinaryNumber(checkBox8.Checked);
            scratchPaper += getBinaryNumber(checkBox7.Checked);
            scratchPaper += getBinaryNumber(checkBox6.Checked);
            scratchPaper += getBinaryNumber(checkBox5.Checked);
            scratchPaper += getBinaryNumber(checkBox4.Checked);
            scratchPaper += getBinaryNumber(checkBox3.Checked);
            scratchPaper += getBinaryNumber(checkBox2.Checked);
            scratchPaper += getBinaryNumber(checkBox1.Checked);
            
            echoChannels = Convert.ToByte(scratchPaper, 2); //convert scratchPaper to a real byte
            CreateEchoCodes();

            if (sfxEnabled) new SoundPlayer(Properties.Resources.ExtraAudio_The_A_Button).Play();
        }

        private string getBinaryNumber(bool input)
        {
            return input ? "1" : "0";
        }

        private void CreateEchoCodes()
        {
            //Updates txtEchoDisplay with:
            //[F5 XX YY YY] [F7 XX YY ZZ]
            //Control code syntax:
            //F5 echoChannels volume_L volume_R
            //F7 delay feedback filter
            var output = $"[F5 {echoChannels:X2} {echoVolume:X2} {echoVolume:X2}] "
                       + $"[F7 {echoDelay:X2} {echoFeedback:X2} {echoFilter:X2}]";

            Clipboard.SetText(output);
            txtEchoDisplay.Text = output;
        }
        
        //Note-related button click events
        private void btnRest_Click(object sender, EventArgs e)
        {
            SendNote(0xC9);
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            SendNote(0xC8);
        }

        private void btnC_Click(object sender, EventArgs e)
        {
            switch (octave)
            {
                case 1:
                    SendNote(0x80);
                    new SoundPlayer(Properties.Resources._1C).Play();
                    break;
                case 2:
                    SendNote(0x8C);
                    new SoundPlayer(Properties.Resources._2C).Play();
                    break;
                case 3:
                    SendNote(0x98);
                    new SoundPlayer(Properties.Resources._3C).Play();
                    break;
                case 4:
                    SendNote(0xA4);
                    new SoundPlayer(Properties.Resources._4C).Play();
                    break;
                case 5:
                    SendNote(0xB0);
                    new SoundPlayer(Properties.Resources._5C).Play();
                    break;
                case 6:
                    SendNote(0xBC);
                    new SoundPlayer(Properties.Resources._6C).Play();
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnCsharp_Click(object sender, EventArgs e)
        {
            switch (octave)
            {
                case 1:
                    SendNote(0x81);
                    new SoundPlayer(Properties.Resources._1Csharp).Play();
                    break;
                case 2:
                    SendNote(0x8D);
                    new SoundPlayer(Properties.Resources._2Csharp).Play();
                    break;
                case 3:
                    SendNote(0x99);
                    new SoundPlayer(Properties.Resources._3Csharp).Play();
                    break;
                case 4:
                    SendNote(0xA5);
                    new SoundPlayer(Properties.Resources._4Csharp).Play();
                    break;
                case 5:
                    SendNote(0xB1);
                    new SoundPlayer(Properties.Resources._5Csharp).Play();
                    break;
                case 6:
                    SendNote(0xBD);
                    new SoundPlayer(Properties.Resources._6Csharp).Play();
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnD_Click(object sender, EventArgs e)
        {
            switch (octave)
            {
                case 1:
                    SendNote(0x82);
                    new SoundPlayer(Properties.Resources._1D).Play();
                    break;
                case 2:
                    SendNote(0x8E);
                    new SoundPlayer(Properties.Resources._2D).Play();
                    break;
                case 3:
                    SendNote(0x9A);
                    new SoundPlayer(Properties.Resources._3D).Play();
                    break;
                case 4:
                    SendNote(0xA6);
                    new SoundPlayer(Properties.Resources._4D).Play();
                    break;
                case 5:
                    SendNote(0xB2);
                    new SoundPlayer(Properties.Resources._5D).Play();
                    break;
                case 6:
                    SendNote(0xBE);
                    new SoundPlayer(Properties.Resources._6D).Play();
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnDsharp_Click(object sender, EventArgs e)
        {
            switch (octave)
            {
                case 1:
                    SendNote(0x83);
                    new SoundPlayer(Properties.Resources._1Dsharp).Play();
                    break;
                case 2:
                    SendNote(0x8F);
                    new SoundPlayer(Properties.Resources._2Dsharp).Play();
                    break;
                case 3:
                    SendNote(0x9B);
                    new SoundPlayer(Properties.Resources._3Dsharp).Play();
                    break;
                case 4:
                    SendNote(0xA7);
                    new SoundPlayer(Properties.Resources._4Dsharp).Play();
                    break;
                case 5:
                    SendNote(0xB3);
                    new SoundPlayer(Properties.Resources._5Dsharp).Play();
                    break;
                case 6:
                    SendNote(0xBF);
                    new SoundPlayer(Properties.Resources._6Dsharp).Play();
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnE_Click(object sender, EventArgs e)
        {
            switch (octave)
            {
                case 1:
                    SendNote(0x84);
                    new SoundPlayer(Properties.Resources._1E).Play();
                    break;
                case 2:
                    SendNote(0x90);
                    new SoundPlayer(Properties.Resources._2E).Play();
                    break;
                case 3:
                    SendNote(0x9C);
                    new SoundPlayer(Properties.Resources._3E).Play();
                    break;
                case 4:
                    SendNote(0xA8);
                    new SoundPlayer(Properties.Resources._4E).Play();
                    break;
                case 5:
                    SendNote(0xB4);
                    new SoundPlayer(Properties.Resources._5E).Play();
                    break;
                case 6:
                    SendNote(0xC0);
                    new SoundPlayer(Properties.Resources._6E).Play();
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnF_Click(object sender, EventArgs e)
        {
            switch (octave)
            {
                case 1:
                    SendNote(0x85);
                    new SoundPlayer(Properties.Resources._1F).Play();
                    break;
                case 2:
                    SendNote(0x91);
                    new SoundPlayer(Properties.Resources._2F).Play();
                    break;
                case 3:
                    SendNote(0x9D);
                    new SoundPlayer(Properties.Resources._3F).Play();
                    break;
                case 4:
                    SendNote(0xA9);
                    new SoundPlayer(Properties.Resources._4F).Play();
                    break;
                case 5:
                    SendNote(0xB5);
                    new SoundPlayer(Properties.Resources._5F).Play();
                    break;
                case 6:
                    SendNote(0xC1);
                    new SoundPlayer(Properties.Resources._6F).Play();
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnFsharp_Click(object sender, EventArgs e)
        {
            switch (octave)
            {
                case 1:
                    SendNote(0x86);
                    new SoundPlayer(Properties.Resources._1Fsharp).Play();
                    break;
                case 2:
                    SendNote(0x92);
                    new SoundPlayer(Properties.Resources._2Fsharp).Play();
                    break;
                case 3:
                    SendNote(0x9E);
                    new SoundPlayer(Properties.Resources._3Fsharp).Play();
                    break;
                case 4:
                    SendNote(0xAA);
                    new SoundPlayer(Properties.Resources._4Fsharp).Play();
                    break;
                case 5:
                    SendNote(0xB6);
                    new SoundPlayer(Properties.Resources._5Fsharp).Play();
                    break;
                case 6:
                    SendNote(0xC2);
                    new SoundPlayer(Properties.Resources._6Fsharp).Play();
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnG_Click(object sender, EventArgs e)
        {
            switch (octave)
            {
                case 1:
                    SendNote(0x87);
                    new SoundPlayer(Properties.Resources._1G).Play();
                    break;
                case 2:
                    SendNote(0x93);
                    new SoundPlayer(Properties.Resources._2G).Play();
                    break;
                case 3:
                    SendNote(0x9F);
                    new SoundPlayer(Properties.Resources._3G).Play();
                    break;
                case 4:
                    SendNote(0xAB);
                    new SoundPlayer(Properties.Resources._4G).Play();
                    break;
                case 5:
                    SendNote(0xB7);
                    new SoundPlayer(Properties.Resources._5G).Play();
                    break;
                case 6:
                    SendNote(0xC3);
                    new SoundPlayer(Properties.Resources._6G).Play();
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnGsharp_Click(object sender, EventArgs e)
        {
            switch (octave)
            {
                case 1:
                    SendNote(0x88);
                    new SoundPlayer(Properties.Resources._1Gsharp).Play();
                    break;
                case 2:
                    SendNote(0x94);
                    new SoundPlayer(Properties.Resources._2Gsharp).Play();
                    break;
                case 3:
                    SendNote(0xA0);
                    new SoundPlayer(Properties.Resources._3Gsharp).Play();
                    break;
                case 4:
                    SendNote(0xAC);
                    new SoundPlayer(Properties.Resources._4Gsharp).Play();
                    break;
                case 5:
                    SendNote(0xB8);
                    new SoundPlayer(Properties.Resources._5Gsharp).Play();
                    break;
                case 6:
                    SendNote(0xC4);
                    new SoundPlayer(Properties.Resources._6Gsharp).Play();
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnA_Click(object sender, EventArgs e)
        {
            switch (octave)
            {
                case 1:
                    SendNote(0x89);
                    new SoundPlayer(Properties.Resources._1zA).Play();
                    break;
                case 2:
                    SendNote(0x95);
                    new SoundPlayer(Properties.Resources._2zA).Play();
                    break;
                case 3:
                    SendNote(0xA1);
                    new SoundPlayer(Properties.Resources._3zA).Play();
                    break;
                case 4:
                    SendNote(0xAD);
                    new SoundPlayer(Properties.Resources._4zA).Play();
                    break;
                case 5:
                    SendNote(0xB9);
                    new SoundPlayer(Properties.Resources._5zA).Play();
                    break;
                case 6:
                    SendNote(0xC5);
                    new SoundPlayer(Properties.Resources._6zA).Play();
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnAsharp_Click(object sender, EventArgs e)
        {
            switch (octave)
            {
                case 1:
                    SendNote(0x8A);
                    new SoundPlayer(Properties.Resources._1zAsharp).Play();
                    break;
                case 2:
                    SendNote(0x96);
                    new SoundPlayer(Properties.Resources._2zAsharp).Play();
                    break;
                case 3:
                    SendNote(0xA2);
                    new SoundPlayer(Properties.Resources._3zAsharp).Play();
                    break;
                case 4:
                    SendNote(0xAE);
                    new SoundPlayer(Properties.Resources._4zAsharp).Play();
                    break;
                case 5:
                    SendNote(0xBA);
                    new SoundPlayer(Properties.Resources._5zAsharp).Play();
                    break;
                case 6:
                    SendNote(0xC6);
                    new SoundPlayer(Properties.Resources._6zAsharp).Play();
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnB_Click(object sender, EventArgs e)
        {
            switch (octave)
            {
                case 1:
                    SendNote(0x8B);
                    new SoundPlayer(Properties.Resources._1zB).Play();
                    break;
                case 2:
                    SendNote(0x97);
                    new SoundPlayer(Properties.Resources._2zB).Play();
                    break;
                case 3:
                    SendNote(0xA3);
                    new SoundPlayer(Properties.Resources._3zB).Play();
                    break;
                case 4:
                    SendNote(0xAF);
                    new SoundPlayer(Properties.Resources._4zB).Play();
                    break;
                case 5:
                    SendNote(0xBB);
                    new SoundPlayer(Properties.Resources._5zB).Play();
                    break;
                case 6:
                    SendNote(0xC7);
                    new SoundPlayer(Properties.Resources._6zB).Play();
                    break;
                default:
                    SendNote("XX");
                    break;
            }
        }

        private void btnOctaveDown_Click(object sender, EventArgs e)
        {
            if (octave <= 1) return;
            octave--;
            OctaveLbl.Text = $"Octave: {octave}";
            if (sfxEnabled) new SoundPlayer(Properties.Resources.ExtraAudio_LeftRight).Play();
        }

        private void btnOctaveUp_Click(object sender, EventArgs e)
        {
            if (octave >= 6) return;
            octave++;
            OctaveLbl.Text = $"Octave: {octave}";
            if (sfxEnabled) new SoundPlayer(Properties.Resources.ExtraAudio_LeftRight).Play();
        }
        
        ///////////////////////////
        //Other button click events
        private void btnChannelTranspose_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[EA " + transposeValue + "]");
        }

        private void btnFinetune1_Click(object sender, EventArgs e)
        {
            //Unfortunately, documenting finetune data in the vanilla ROM is quite an undertaking...
            //Just look at other songs for reference :(
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[F4 00]");
        }

        private void btnCopySlidingPan_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            FormatNoteLength();
            var output = $"[E2 {noteLength:X2} {Math.Abs(PanningBar.Value):X2}]"; //[E2 length panning]
            Clipboard.SetText(output);
        }

        private void btnCopySlidingVolume_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            FormatNoteLength();
            var output = $"[EE {noteLength:X2} {Math.Abs(ChannelVolumeBar.Value):X2}]"; //[EE length volume]
            Clipboard.SetText(output);
        }

        private void btnCopySlidingEcho_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            FormatNoteLength();
            var vol = Math.Abs(ChannelVolumeBar.Value).ToString("X2");
            var output = $"[F8 {noteLength:X2} {vol} {vol}]"; //[F8 length vol vol]
            Clipboard.SetText(output);
        }

        private void btnEchoOff_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[F6]");
        }

        private void btnTempo_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[E7 20]");
        }

        private void btnGlobalVolume_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[E5 F0]");
        }

        private void btnSetFirstDrum_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[FA 00]");
        }

        private void btnPortamentoUp_Click(object sender, EventArgs e)
        {
            //[F1 start length range]
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[F1 00 06 01]");
        }

        private void btnPortamentoDown_Click(object sender, EventArgs e)
        {
            //[F2 start length range]
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[F2 00 06 01]");
        }

        private void btnPortamentoOff_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[F3]");
        }

        private void btnPortamento_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("C8 [F9 00 01 ");
        }

        private void btnVibrato_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[E3 0C 1C 32]");
        }

        private void btnVibratoOff_Click(object sender, EventArgs e)
        {
            if (sfxEnabled) sfxEquipped.Play();
            Clipboard.SetText("[E4]");
        }
        
        private void PanningBar_Scroll(object sender, EventArgs e)
        {
            FormatNoteLength();
            var panPosition = PanningBar.Value;
            panPosition = Math.Abs(panPosition); //They're negative numbers, so this makes them positive (takes the absolute value)
            var output = $"[E1 {panPosition:X2}]"; //[E1 panning]
            txtPanningDisplay.Text = output;
            Clipboard.SetText(output);
            if (sfxEnabled) sfxTextBlip.Play();
        }

        private void ChannelVolumeBar_Scroll(object sender, EventArgs e)
        {
            FormatNoteLength();
            //ChannelVolumeBar and txtChannelVolumeDisplay
            var volume = ChannelVolumeBar.Value;
            var output = $"[ED {volume:X2}]"; //[ED volume]
            txtChannelVolumeDisplay.Text = output;
            Clipboard.SetText(output);
            if (sfxEnabled) PlayTextTypeSound("huge");
        }

        private void StaccatoBar_Scroll(object sender, EventArgs e)
        {
            noteStacatto = (byte)StaccatoBar.Value;
            LengthDisplay.Text = FormatNoteLength();
            if (sfxEnabled) sfxTextBlip.Play();
        }

        private void VolBar_Scroll(object sender, EventArgs e)
        {
            noteVolume = (byte)VolBar.Value;
            LengthDisplay.Text = FormatNoteLength();
            if (sfxEnabled) PlayTextTypeSound("tiny");
        }

        private void btnTremolo_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This control code is currently unimplemented.");
        }

        private void btnTremoloOff_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This control code is currently unimplemented.");
        }

        private void btnMPTconvert_Click(object sender, EventArgs e)
        {
            //TODO: Validation

            var mptColumnText = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(mptColumnText)) return;

            var result = MPTColumn.GetEBMdata(mptColumnText); //convert the OpenMPT note data to N-SPC format
            Clipboard.SetText(result);
            if (sfxEnabled) sfxEquipped.Play();
        }

        private void btnC8eraser_Click(object sender, EventArgs e)
        {
            int originalLength = 0x06; //TODO: Make a control that lets the user choose which length to have here

            //VALIDATION
            string input = Clipboard.GetText();

            if (string.IsNullOrWhiteSpace(input)) return; //only continue if there's something there
            
            //split the contents on spaces to a string array
            input = input.Replace("[", ""); //get rid of any brackets
            input = input.Replace("]", "");

            //Clear out any lingering note length commands in the text itself
            var firstCode = Convert.ToInt32(input.Substring(0, 2), 16); //convert the first code in the clipboard to an int
            if (firstCode < 0x80) //any code less than 80 (C-1) is a note length and should be removed
            {
                input = input.Replace(firstCode.ToString("X2") + " ", "");
                originalLength = firstCode; //if it's not 06, it should be set appropriately so later on the right value gets put at the end of the clipboard result
            }
            
            //Make a string array with all of the notes in it
            var notes = input.Split(' ');

            if (notes.Length <= 1)
            {
                MessageBox.Show("This looks like it's just one note. No changes necessary here!");
                return;
            }
            
            //check to see that only the first one is not C8
            for (var i = 1; i < notes.Length; i++)
            {
                if (notes[i] != "C8")
                {
                    MessageBox.Show("Looks like there's more than one note in here...\r\nStripping multiple notes hasn't been implemented yet.\r\n(The note in question is " + notes[i] + ")");
                    //TODO: Implement the stripping of multiple notes at once.
                    //This would involve lots of note length shenanigans, though... It might not be worth doing.
                    return;
                }
            }

            var count = notes.Length;
            
            //returns [new length, appropriate multiplier]
            var newLength = EBM_Note_Data.validateNoteLength(originalLength * count);

            var message = $"Number of notes: {count}\r\n"
                        + "Equivalent note length: ";
            
            if (newLength[1] != 1)
                message += $"[{newLength[0]:X2}], {getWrittenNumber(newLength[1])} times.";
            else
                message += $"[{newLength[0]:X2}]";

            MessageBox.Show(message);

            //set the clipboard to the new length, whatever note is at the start of what was copied, and then the original length so when you paste it in, the rest of the column doesn't go out of whack
            var result = new StringBuilder();
            result.Append(newLength[0].ToString("X2")); //the length that the new 
            result.Append(" ");
            result.Append(notes[0]); //The note at the top of the clipboard contents
            result.Append(" ");
            
            while (newLength[1] > 1) //this should only run if 
            {
                result.Append("C8 "); //put in however many C8s are needed to make this the same size as the original clipboard contents
                newLength[1]--;
            }
            result.Append(originalLength.ToString("X2")); //the 06 so things don't jump around when you paste it in
            
            Clipboard.SetText(result.ToString()); //paste this into EBMusEd for glorious ease of use
            if (sfxEnabled) sfxEquipped.Play();
        }



        //Text blip stuff
        //If the sfxEnabled boolean value is set to true, then various parts of the UI will give audio feedback.
        //I'm getting kind of tired of it, though, so I'm going to make it toggleable in the future.
        byte numberOfLettersBeforeSound;
        SoundPlayer sfxTextBlip = new SoundPlayer(Properties.Resources.ExtraAudio_Text_Blip); //adding these so it doesn't make a new instance of SoundPlayer *every* time
        SoundPlayer sfxEquipped = new SoundPlayer(Properties.Resources.ExtraAudio_Equipped_);

        private void PlayTextTypeSound(string type)
        {
            if (!sfxEnabled) return;

            //Text blip logic
            byte amount = 1;
            if (type == "huge") amount = 5;

            if (numberOfLettersBeforeSound > amount)
            {
                numberOfLettersBeforeSound = 0;
                sfxTextBlip.Play();
            }
            else numberOfLettersBeforeSound++;
        }

        private void trackBarEchoVol_Scroll(object sender, EventArgs e)
        {
            SetAllEchoValues();
            CreateEchoCodes();
            if (sfxEnabled) PlayTextTypeSound("huge");
        }

        private void trackBarEchoDelay_Scroll(object sender, EventArgs e)
        {
            SetAllEchoValues();
            CreateEchoCodes();
            if (sfxEnabled) PlayTextTypeSound("tiny");
        }

        private void trackBarEchoFeedback_Scroll(object sender, EventArgs e)
        {
            SetAllEchoValues();
            CreateEchoCodes();
            if (sfxEnabled) PlayTextTypeSound("huge");
        }

        private void trackBarEchoFilter_Scroll(object sender, EventArgs e)
        {
            SetAllEchoValues();
            CreateEchoCodes();
            if (sfxEnabled) PlayTextTypeSound("tiny");
        }


        //All of the echo-related checkboxes redirect to calculateEchoChannelCode() to provide immediate feedback
        private void checkBox1_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); }
        private void checkBox2_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); }
        private void checkBox3_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); } 
        private void checkBox4_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); } 
        private void checkBox5_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); } 
        private void checkBox6_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); }
        private void checkBox7_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); }
        private void checkBox8_CheckedChanged(object sender, EventArgs e) { CalculateEchoChannelCode(); }

        private void chkMiscFeedback_CheckedChanged(object sender, EventArgs e)
        {
            if (chkMiscFeedback.Checked)
            {
                sfxEnabled = true; //this is one of the global variables defined at the beginning
                new SoundPlayer(Properties.Resources.ExtraAudio_LeftRight).Play();
            }
            else sfxEnabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtMultiplier.TextChanged += txtMultiplier_TextChanged; //The default click event for txtMultiplier doesn't do anything, so this is the next best alternative

            //Tooltip stuff from http://stackoverflow.com/questions/1339524/c-how-do-i-add-a-tooltip-to-a-control
            var toolTip1 = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 300,
                ReshowDelay = 700,
                ShowAlways = true
            };

            toolTip1.SetToolTip(trackBarEchoDelay, "The higher this is, the less space you have for samples and note data.\r\nBe sure to test your song in an accurate emulator!");
            toolTip1.SetToolTip(btnVibrato, "[E3 start speed range]");
            toolTip1.SetToolTip(btnPortamentoUp, "Plays the note, THEN bends the pitch.\r\n[F1 start length range]");
            toolTip1.SetToolTip(btnPortamentoDown, "Bends the pitch INTO the note.\r\n[F2 start length range]");
            toolTip1.SetToolTip(checkBox8, "Watch out! \nThis one's used for sound effects.");
            toolTip1.SetToolTip(btnCopySlidingPan, "[E2 length panning]");
            toolTip1.SetToolTip(btnCopySlidingVolume, "[EE length volume]");
            toolTip1.SetToolTip(btnCopySlidingEcho, "[F8 length lvol rvol]");
            toolTip1.SetToolTip(btnPortamento, "C8 [F9 start length (insert note here)] ");
            toolTip1.SetToolTip(btnSetFirstDrum, "Sets the first sample used by the CA-DF note system.\r\nThis is useful for making quick drum loops.");
            toolTip1.SetToolTip(trackBarEchoVol, "The second half of volume levels invert the waveform!\r\nYou can set the left and right numbers separately, too.");
            //toolTip1.SetToolTip(ANYTHING, "");
            //toolTip1.SetToolTip(ANYTHING, "");
            //toolTip1.SetToolTip(ANYTHING, "");
            //toolTip1.SetToolTip(ANYTHING, "");
            //toolTip1.SetToolTip(ANYTHING, "");
        }
    }
}