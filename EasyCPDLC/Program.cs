/*  EASYCPDLC: CPDLC Client for the VATSIM Network
    Copyright (C) 2021 Joshua Seagrave joshseagrave@googlemail.com

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    at your option any later version.
*/

using System;
using System.Windows.Forms;

namespace EasyCPDLC
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Idle += (_, __) => EasyCPDLCAppIcon.ApplyOpenForms();

            MainForm mainForm = new MainForm();
            EasyCPDLCAppIcon.Apply(mainForm);

            bool startWithVns430 = Array.Exists(
                args ?? Array.Empty<string>(),
                value => string.Equals(value, "--vns430", StringComparison.OrdinalIgnoreCase));

            if (startWithVns430)
            {
                mainForm.Shown += (_, __) => mainForm.BeginInvoke(new Action(() =>
                {
                    mainForm.ShowVns430Panel();
                    mainForm.Hide();
                }));
            }

            Application.Run(mainForm);
        }
    }
}
