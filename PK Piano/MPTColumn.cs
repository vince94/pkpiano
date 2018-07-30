﻿using System;
using System.Text;

namespace PK_Piano
{
    //These classes are a very recent addition. TODO: Move more of the logic to classes like this
    class MPTColumn
    {
        public static string GetEBMdata(string input)
        {
            StringBuilder result = new StringBuilder();
            string[] rows = input.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            string rowStatus = validation(rows);
            if (rowStatus != "Valid!") return rowStatus; //display the error message if something's up

            foreach (string row in rows)
            {
                if (row.StartsWith("ModPlug Tracker"))
                    result.Append(""); //skip the header row
                else
                    result.Append(EBM_Note_Data.GetEBMNote(row)); //convert all of the notes
            }

            return result.ToString();
        }

        //TODO: Figure out what hex values AddMusicK uses and implement it as "GetAMKdate(string input)"

        public static string validation(string[] input)
        {
            string errorMessage = "Valid!";

            if (input.Length < 2)
                errorMessage = "(Not enough rows to process)";
            else if (input[0].Length < 15) //this doesn't work if someone pastes a really long thing in from something else...
                errorMessage = "(That doesn't look like something pasted from OpenMPT)";
            else if (input[1].Length > 12)
                errorMessage = "(Clipboard contains more than one column of notes: Length is " + input[1].Length + ")";

            return errorMessage;
        }
    }
}