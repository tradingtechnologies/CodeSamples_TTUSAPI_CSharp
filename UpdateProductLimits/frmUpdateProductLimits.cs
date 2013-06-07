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
    public partial class frmUpdateProductLimits : Form
    {

        


        private TTUSAPI.TTUSApi m_TTUSAPI;

        private Dictionary<int, TTUSAPI.DataObjects.AccountGroup> m_accountGroups;  // Account groups
        private Dictionary<int, TTUSAPI.DataObjects.Gateway> m_colGateways;  // Gateway information


        public frmUpdateProductLimits()
        {
            InitializeComponent();
        }

        public void initTTUSAPI()
        {
            m_TTUSAPI = new TTUSAPI.TTUSApi(TTUSAPI.TTUSApi.StartupMode.Normal);  //Create an Instance of the TTUSAPI
            //Callbacks
            m_TTUSAPI.OnConnectivityStatusUpdate += new TTUSApi.ConnectivityStatusUpdateHandler(m_TTUSAPI_OnConnectivityStatusUpdate);
            m_TTUSAPI.OnLoginStatusUpdate += new TTUSApi.LoginStatusHandler(m_TTUSAPI_OnLoginStatusUpdate);  //Login status update
            m_TTUSAPI.OnInitializeComplete += new TTUSApi.InitializeCompleteHandler(m_TTUSAPI_OnInitializeComplete); //API Initialization
            m_TTUSAPI.OnAccountGroupUpdate += new TTUSApi.AccountGroupUpdateHandler(m_TTUSAPI_OnAccountGroupUpdate); //Account group updates
            m_TTUSAPI.OnGatewayUpdate += new TTUSApi.GatewayUpdateHandler(m_TTUSAPI_OnGatewayUpdate); //Gateway information

            btnConnect.Enabled = false;
            btnUpdate.Enabled = false;
        }

        #region TTUS Callbacks

        void m_TTUSAPI_OnConnectivityStatusUpdate(object sender, ConnectivityStatusEventArgs e)
        {
            UpdateStatusBar("Found a TT User Setup Server, You can now login...");
            this.btnConnect.Enabled = true;
        }

        void m_TTUSAPI_OnLoginStatusUpdate(object sender, LoginStatusEventArgs e)
        {
            if (e.LoginResultCode == TTUSAPI.LoginResultCode.Success)
            {
                Console.WriteLine("Login was Successful");
                //We have successfully logged in, so request users and FA Servers...
                m_TTUSAPI.Initialize();   //Initialize the API to get all of the User, Fix Adapter, Account, and MGT data
                m_TTUSAPI.GetProducts();
            }
            else
            {
                Console.WriteLine("Error:  Login failed");
                Environment.Exit(0);
            }
        }

        void m_TTUSAPI_OnInitializeComplete(object sender, TTUSAPI.InitializeCompleteEventArgs e)
        {
            UpdateStatusBar("Initialization Complete.");
            btnUpdate.Enabled = true;
        }


        void m_TTUSAPI_OnAccountGroupUpdate(object sender, AccountGroupUpdateEventArgs e)
        {
            try
            {
                object previousItem = cbAccount.SelectedItem;
                cbAccount.Items.Clear();
                // Populate dictionary with Account Groups
                if (e.Type == UpdateType.Download)
                    m_accountGroups = e.AccountGroups;
                // Add Account Group
                else if (e.Type == UpdateType.Added)
                {
                    foreach (KeyValuePair<int, TTUSAPI.DataObjects.AccountGroup> acctGroup in e.AccountGroups)
                    {
                        m_accountGroups[acctGroup.Key] = acctGroup.Value;
                    }
                }
                // Update Account Group
                else if ((e.Type == UpdateType.Changed || e.Type == UpdateType.Relationship))
                {
                    foreach (KeyValuePair<int, TTUSAPI.DataObjects.AccountGroup> acctGroup in e.AccountGroups)
                    {
                        m_accountGroups[acctGroup.Key] = acctGroup.Value;
                    }
                }
                // Delete Account Group
                else if (e.Type == UpdateType.Deleted)
                {
                    foreach (KeyValuePair<int, TTUSAPI.DataObjects.AccountGroup> acctGroup in e.AccountGroups)
                    {
                        m_accountGroups.Remove(acctGroup.Key);
                    }
                }

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

        // Populate dictionary of Gateway Information
        void m_TTUSAPI_OnGatewayUpdate(object sender, GatewayUpdateEventArgs e)
        {
            m_colGateways = e.Gateways;
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


        //For a given product type, return the proper ID number
        private int getProductTypeId(string productType)
        {
            if (productType.ToUpper().Equals("FUTURE"))
                return 1;
            else if (productType.ToUpper().Equals("SPREAD"))
                return 2;
            else if (productType.ToUpper().Equals("OPTION"))
                return 3;
            else
                return -1;
        }

        //Retrieve the ID for a given exchange name
        private int getGatewayId(string exchange)
        {
            foreach (TTUSAPI.DataObjects.Gateway gw in m_colGateways.Values) //Loop through gateways to map gateway name to gateway ID
            {
                if (gw.GatewayName == txtExchange.Text.Trim())
                {
                    return gw.GatewayID;
                }
            }
            return -1;
        }

        #region FormEventHandlers

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            int gatewayId = getGatewayId(txtExchange.Text);
            int productTypeId = getProductTypeId(cbProductType.SelectedItem.ToString());
            uint maxPos = 0;
            uint maxLongShort = 0;
            
            if(gatewayId != -1 && productTypeId != -1 && uint.TryParse(txtMaxPosition.Text, out maxPos) && uint.TryParse(txtMaxLongShort.Text, out maxLongShort))
            {
                foreach (TTUSAPI.DataObjects.AccountGroup acctGroup in m_accountGroups.Values) // Loop through downloaded account groups
                {
                    if (acctGroup.Name == cbAccount.SelectedItem.ToString())
                    {
                        TTUSAPI.DataObjects.AccountGroupProfile acctGroupProfile = new TTUSAPI.DataObjects.AccountGroupProfile(acctGroup);

                        TTUSAPI.DataObjects.ProductLimitProfile productLimit;
                        bool productLimitSet = false;
                        foreach (TTUSAPI.DataObjects.ProductLimit prodLimit in acctGroupProfile.ProductLimits.Values) //Loop through product limits associated with the account
                        {
                            //Replace account product limits that match ones in the update file
                            if (prodLimit.GatewayID == gatewayId && prodLimit.ProductTypeID == productTypeId && prodLimit.Product.ToUpper().Equals(txtProduct.Text.ToUpper()))
                            {
                                productLimit = new TTUSAPI.DataObjects.ProductLimitProfile(prodLimit); //Create copy of Product Limit
                                productLimit.MaxPosition = maxPos; //Set Maximum Position
                                productLimit.MaxLongShort = maxLongShort; // Set Maximum Long/Short Position
                                acctGroupProfile.AddProductLimit(productLimit); //Add product limit to account
                                productLimitSet = true;
                                break;
                            }
                        }
                        //Create new product limits if they don't currently exist for the account
                        if (productLimitSet == false)
                        {
                            productLimit = new TTUSAPI.DataObjects.ProductLimitProfile(); //Create new Product Limit
                            //Assign values to product limit from update file object
                            productLimit.GatewayID = gatewayId;
                            productLimit.Product = txtProduct.Text;
                            productLimit.ProductTypeID = productTypeId;
                            productLimit.MaxPosition = maxPos;
                            productLimit.MaxLongShort = maxLongShort;
                            productLimit.TradingAllowed = true;
                            acctGroupProfile.AddProductLimit(productLimit); //Add product limit to account
                        }

                        ResultStatus res = m_TTUSAPI.UpdateAccountGroup(acctGroupProfile); //Send updates to TTUS Server
                        if (res.Result == ResultType.SentToServer) //On Success
                        {
                            UpdateStatusBar("Updated account " + acctGroup.Name);
                        }
                        else if (res.Result == ResultType.ValuesUnchanged) //No Change
                        {
                            UpdateStatusBar("Account " + acctGroup.Name + " unchanged");
                        }
                        else //On Failure
                        {
                            UpdateStatusBar("Failed to update account " + acctGroup.Name + ", " + res.ErrorMessage);
                        }
                    }
                }
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            m_TTUSAPI.Login(txtUsername.Text, txtPassword.Text);
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
