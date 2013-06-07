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
    public partial class frmLockAccount : Form
    {

        private TTUSAPI.TTUSApi m_TTUSAPI;

        //Dictionaries to store TTUS info
        private Dictionary<int, TTUSAPI.DataObjects.AccountGroup> m_accountGroups;  //Account groups

        public frmLockAccount()
        {
            InitializeComponent();
        }

        public void initTTUSAPI()
        {

            UpdateStatusBar("Initializing...");
            m_TTUSAPI = new TTUSAPI.TTUSApi(TTUSAPI.TTUSApi.StartupMode.Normal);  //Create an Instance of the TTUSAPI
            m_TTUSAPI.OnConnectivityStatusUpdate += new TTUSApi.ConnectivityStatusUpdateHandler(m_TTUSAPI_OnConnectivityStatusUpdate); //Callback for getting TTUS Server status
            m_TTUSAPI.OnLoginStatusUpdate += new TTUSApi.LoginStatusHandler(m_TTUSAPI_OnLoginStatusUpdate);  //Callback for login update
            m_TTUSAPI.OnInitializeComplete += new TTUSApi.InitializeCompleteHandler(m_TTUSAPI_OnInitializeComplete);  //Callback for api initialization
            m_TTUSAPI.OnAccountGroupUpdate += new TTUSApi.AccountGroupUpdateHandler(m_TTUSAPI_OnAccountGroupUpdate); //Callback for Account group updates
            this.btnConnect.Enabled = false;
            this.btnLockAccount.Enabled = false;
        }


        #region TTUS Callbacks

        //Callback for TTUS Server Status Updates
        void m_TTUSAPI_OnConnectivityStatusUpdate(object sender, ConnectivityStatusEventArgs e)
        {
            //We detected a TTUS server so now we can login
            UpdateStatusBar("Found a TT User Setup Server, You can now login...");
            this.btnConnect.Enabled = true;
        }

        //Callback for TTUS Server Login Status Updates
        void m_TTUSAPI_OnLoginStatusUpdate(object sender, LoginStatusEventArgs e)
        {
            if (e.LoginResultCode == TTUSAPI.LoginResultCode.Success)
            {
                UpdateStatusBar("Login was Successful");
                
                m_TTUSAPI.Initialize();  
            }
            else
            {
                UpdateStatusBar("Error:  Login failed");
            }
        }

        //Callback for TTUS API Initialization
        void m_TTUSAPI_OnInitializeComplete(object sender, TTUSAPI.InitializeCompleteEventArgs e)
        {
            UpdateStatusBar("Initialization Complete.");
            this.btnLockAccount.Enabled = true;
        }

        //Callback for downloading and updating account groups
        void m_TTUSAPI_OnAccountGroupUpdate(object sender, AccountGroupUpdateEventArgs e)
        {

            try
            {
                object previousItem = cbAccount.SelectedItem;
                cbAccount.Items.Clear();
                //Populate dictionary with downloaded accounts
                if (e.Type == UpdateType.Download)
                {
                    m_accountGroups = e.AccountGroups;
                }
                //Update dictionary with any user updates
                else if (e.Type == UpdateType.Added || e.Type == UpdateType.Changed || e.Type == UpdateType.Relationship)
                {
                    foreach (KeyValuePair<int, TTUSAPI.DataObjects.AccountGroup> acctItem in e.AccountGroups)
                    {
                        m_accountGroups[acctItem.Key] = acctItem.Value;
                    }
                }
                //Remove user from dictionary
                else if (e.Type == UpdateType.Deleted)
                {
                    foreach (KeyValuePair<int, TTUSAPI.DataObjects.AccountGroup> acctItem in e.AccountGroups)
                    {
                        m_accountGroups.Remove(acctItem.Key);
                    }
                }
                //Display all users
                foreach (TTUSAPI.DataObjects.AccountGroup acct in m_accountGroups.Values)
                {
                    cbAccount.Items.Add(acct.Name);
                }
                if (previousItem != null && cbAccount.Items.Contains(previousItem))
                    cbAccount.SelectedItem = previousItem;
                else if (previousItem == null)
                    cbAccount.SelectedIndex = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an exception in the OnAccountGroupUpdate callback: " + ex.Message);
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


        #region FormEventHandlers

        private void btnConnect_Click(object sender, EventArgs e)
        {
            m_TTUSAPI.Login(txtUsername.Text, txtPassword.Text);
        }

        private void btnLockAccount_Click(object sender, EventArgs e)
        {
            foreach (TTUSAPI.DataObjects.AccountGroup acctGroup in m_accountGroups.Values) // Loop through downloaded account groups
            {
                if (acctGroup.Name == cbAccount.SelectedItem.ToString())
                {
                    TTUSAPI.DataObjects.AccountGroupProfile acctGroupP = new TTUSAPI.DataObjects.AccountGroupProfile(acctGroup); // Create copy of the downloaded Account Group
                    acctGroupP.TradingAllowed = false; // Disable Trading for account
                    ResultStatus res = m_TTUSAPI.UpdateAccountGroup(acctGroupP);  // Send update to TTUS Server
                    if (res.Result == ResultType.SentToServer) //On Success
                    {
                        UpdateStatusBar("Trading disabled for Account " + acctGroup.Name);
                    }
                    else if (res.Result == ResultType.ValuesUnchanged) //On No Change
                    {
                        UpdateStatusBar("Trading already disable for Account " + acctGroup.Name);
                    }
                    else //On Failure
                    {
                        UpdateStatusBar("ERROR:  Failed to disable trading for Account " + acctGroup.Name + ", " + res.ErrorMessage);
                    }
                }

            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutDTS aboutForm = new AboutDTS();
            aboutForm.ShowDialog(this);
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
    }
}
