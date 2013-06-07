using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using TTUSAPI;

namespace TTUSAPI_Samples
{
    public partial class frmUpdateCredit : Form
    {

        private TTUSAPI.TTUSApi m_TTUSAPI;

        //Dictionaries to store user info
        private Dictionary<string, TTUSAPI.DataObjects.User> m_Users;

        public frmUpdateCredit()
        {
            InitializeComponent();
        }

        public void initTTUSAPI()
        {
            m_TTUSAPI = new TTUSAPI.TTUSApi(TTUSAPI.TTUSApi.StartupMode.Normal);  //Create an Instance of the TTUSAPI
            m_TTUSAPI.OnConnectivityStatusUpdate += new TTUSApi.ConnectivityStatusUpdateHandler(m_TTUSAPI_OnConnectivityStatusUpdate); //Callback for getting TTUS Server status
            m_TTUSAPI.OnLoginStatusUpdate += new TTUSApi.LoginStatusHandler(m_TTUSAPI_OnLoginStatusUpdate);  //Callback for login update
            m_TTUSAPI.OnInitializeComplete += new TTUSApi.InitializeCompleteHandler(m_TTUSAPI_OnInitializeComplete);  //Callback for initialization complete
            m_TTUSAPI.OnUserUpdate += new TTUSApi.UserUpdateHandler(m_TTUSAPI_OnUserUpdate);  //Callback for user downloads
            this.btnConnect.Enabled = false;
            this.btnUpdate.Enabled = false;
        }

        #region TTUS Callbacks

        //Callback for TTUS Server Status Updates
        void m_TTUSAPI_OnConnectivityStatusUpdate(object sender, ConnectivityStatusEventArgs e)
        {
            UpdateStatusBar("Found a TT User Setup Server, You can now login...");
            this.btnConnect.Enabled = true;
        }

        //Callback for TTUS Server Login Status Updates
        void m_TTUSAPI_OnLoginStatusUpdate(object sender, LoginStatusEventArgs e)
        {
            if (e.LoginResultCode == TTUSAPI.LoginResultCode.Success)
            {
                Console.WriteLine("Login was Successful");
                //We have successfully logged in, so request users
                m_TTUSAPI.Initialize();   //Initialize the API to get all of the User, Fix Adapter, Account, and MGT data
            }
            else
            {
                Console.WriteLine("Error:  Login failed");
            }
        }

        //Callback for TTUS API Initialization
        void m_TTUSAPI_OnInitializeComplete(object sender, TTUSAPI.InitializeCompleteEventArgs e)
        {
            //The API is initialized so now we can read the file and update the credit limits
            UpdateStatusBar("Initialization Complete.");
            btnUpdate.Enabled = true;
        }

        //Callback for TTUS User Updates
        void m_TTUSAPI_OnUserUpdate(object sender, UserUpdateEventArgs e)
        {
            try
            {
                object previousItem = cbUser.SelectedItem;
                cbUser.Items.Clear();
                //Populate dictionary with downloaded users
                if (e.Type == UpdateType.Download)
                {
                    m_Users = e.Users;
                }
                //Update dictionary with any user updates
                else if (e.Type == UpdateType.Added || e.Type == UpdateType.Changed || e.Type == UpdateType.Relationship)
                {
                    foreach (KeyValuePair<string, TTUSAPI.DataObjects.User> userItem in e.Users)
                    {
                        m_Users[userItem.Key] = userItem.Value;
                    }
                }
                //Remove user from dictionary
                else if (e.Type == UpdateType.Deleted)
                {
                    foreach (KeyValuePair<string, TTUSAPI.DataObjects.User> userItem in e.Users)
                    {
                        m_Users.Remove(userItem.Key);
                    }
                }
                //Display all users
                foreach (TTUSAPI.DataObjects.User user in m_Users.Values)
                {
                    cbUser.Items.Add(user.UserName);
                }
                if (previousItem != null && cbUser.Items.Contains(previousItem))
                    cbUser.SelectedItem = previousItem;
                else if (previousItem == null)
                    cbUser.SelectedIndex = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an exception in the OnUserUpdate callback: " + ex.Message);
            }

        }

        #endregion

        #region UpdateUIRegion



        /// <summary>
        /// Update the status bar and write the message to the console in a thread safe way.
        /// </summary>
        /// <param name="message">Message to update the status bar with.</param>
        delegate void UpdateStatusBarCallback(string message);
        public void UpdateStatusBar(string message)
        {
            if (this.InvokeRequired)
            {
                UpdateStatusBarCallback statCB = new UpdateStatusBarCallback(UpdateStatusBar);
                this.Invoke(statCB, new object[] { message });
            }
            else
            {
                // Update the status bar.
                toolStripStatusLabel1.Text = message;

                // Also write this message to the console.
                Console.WriteLine(message);
            }
        }

        #endregion

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            // shut down the API
            m_TTUSAPI.Logoff();  // Logoff the TTUS Server
            m_TTUSAPI.Dispose();

            base.Dispose(disposing);
        }

        #region FormEventHandlers

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutDTS aboutForm = new AboutDTS();
            aboutForm.ShowDialog(this);	
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            m_TTUSAPI.Login(txtUsername.Text, txtPassword.Text);
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            int creditLimit = 0;
            foreach (TTUSAPI.DataObjects.User user in m_Users.Values)  //Loop through all users that have been downloaded
            {
                    if (user.UserName == cbUser.SelectedItem.ToString() && Int32.TryParse(txtCreditLimit.Text, out creditLimit))
                    {
                        TTUSAPI.DataObjects.UserProfile userUpdate = new TTUSAPI.DataObjects.UserProfile(user);  // Create a copy of the user
                        userUpdate.UserRiskSettings.Credit = creditLimit;  // Update the user credit limit
                        ResultStatus res = m_TTUSAPI.UpdateUser(userUpdate);  // Send update to TTUS server
                        if (res.Result == ResultType.SentToServer) // On Success
                        {
                            UpdateStatusBar("Credit updated for " + user.UserName + " to " + creditLimit + " " + userUpdate.UserRiskSettings.Currency.Name);
                        }
                        else if (res.Result == ResultType.ValuesUnchanged) // On No Change
                        {
                            UpdateStatusBar("Credit for " + user.UserName + " unchanged at " + creditLimit + " " + userUpdate.UserRiskSettings.Currency.Name);
                        }
                        else //On Failure
                        {
                            UpdateStatusBar("Failed to update credit for user " + user.UserName + ", " + res.ErrorMessage);
                        }
                    }
                
            }
        }

        #endregion

    }
}
