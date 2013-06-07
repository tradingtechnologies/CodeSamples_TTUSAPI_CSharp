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
    public partial class frmUpdateCurrency : Form
    {

        private TTUSApi m_TTUSAPI;

        private Dictionary<string, TTUSAPI.DataObjects.Currency> m_lstCurrencies;
        private Dictionary<string, TTUSAPI.DataObjects.CurrencyExchangeRate> m_currencyExchange;


        public frmUpdateCurrency()
        {
            InitializeComponent();
        }

        public void initTTUSAPI()
        {
            UpdateStatusBar("Initializing...");
            m_TTUSAPI = new TTUSApi(TTUSAPI.TTUSApi.StartupMode.Normal);  //Create an Instance of the TTUSAPI
            m_TTUSAPI.OnConnectivityStatusUpdate += new TTUSApi.ConnectivityStatusUpdateHandler(m_TTUSAPI_OnConnectivityStatusUpdate); //Callback for getting TTUS Server status
            m_TTUSAPI.OnLoginStatusUpdate += new TTUSApi.LoginStatusHandler(m_TTUSAPI_OnLoginStatusUpdate);  //Callback for login update
            m_TTUSAPI.OnInitializeComplete += new TTUSApi.InitializeCompleteHandler(m_TTUSAPI_OnInitializeComplete);  //Callback for api initialization
            m_TTUSAPI.OnCurrencyUpdate += new TTUSApi.CurrencyUpdateHandler(m_TTUSAPI_OnCurrencyUpdate);
            m_TTUSAPI.OnCurrencyExchangeRateUpdate += new TTUSApi.CurrencyExchangeRateUpdateHandler(m_TTUSAPI_OnCurrencyExchangeRateUpdate);
            this.btnConnect.Enabled = false;
            this.btnUpdateRate.Enabled = false;
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
                UpdateStatusBar("Login was Successful");
                //We have successfully logged in, so request users
                m_TTUSAPI.Initialize();   //Initialize the API to get all of the User, Fix Adapter, Account, and MGT data
            }
            else
            {
                UpdateStatusBar("Error:  Login failed");
            }
        }

        //Callback for TTUS API Initialization
        void m_TTUSAPI_OnInitializeComplete(object sender, TTUSAPI.InitializeCompleteEventArgs e)
        {
            UpdateStatusBar("Initialization Complete. Ready to update currencies");

            //Populate our currency drop down boxes
            cbCurrency1.Items.Clear();
            cbCurrency2.Items.Clear();
            foreach (string currency in m_lstCurrencies.Keys)
            {
                cbCurrency1.Items.Add(currency);
                cbCurrency2.Items.Add(currency);
            }

            this.btnUpdateRate.Enabled = true;
        }


        //Callback for Currency Update
        void m_TTUSAPI_OnCurrencyUpdate(object sender, CurrencyUpdateEventArgs e)
        {
            m_lstCurrencies = e.Currencies;
        }

        void m_TTUSAPI_OnCurrencyExchangeRateUpdate(object sender, CurrencyExchangeRateUpdateEventArgs e)
        {
            m_currencyExchange = e.CurrencyExchangeRates;
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

        private void btnUpdateRate_Click(object sender, EventArgs e)
        {
            try
            {
                string currency1 = cbCurrency1.SelectedItem as string;
                string currency2 = cbCurrency2.SelectedItem as string;
                double rate_d = 1.0;
                if (!Double.TryParse(txtRate.Text, out rate_d) || rate_d < 0)
                {
                    MessageBox.Show("Please check the conversion rate, as it does not appear to be valid.");
                    return;
                }

                if (currency1 == currency2)
                {
                    MessageBox.Show("No need to update rate between the same currency");
                    return;
                }

                //Container for our updates
                HashSet<TTUSAPI.DataObjects.CurrencyExchangeRateProfile> currencyH = new HashSet<TTUSAPI.DataObjects.CurrencyExchangeRateProfile>();
                bool foundExchangeRate = false;
                string exchangeRateName = currency1 + "_" + currency2;

                foreach (TTUSAPI.DataObjects.CurrencyExchangeRate exchangeRate in m_currencyExchange.Values) // Loop through downloaded exchange rates
                {
                    if (exchangeRate.Name == exchangeRateName)  // Check for match between user selected exchange rate and downloaded exchange rates
                    {
                        TTUSAPI.DataObjects.CurrencyExchangeRateProfile exchangeRateP = new TTUSAPI.DataObjects.CurrencyExchangeRateProfile(exchangeRate);  // Make copy of exchange rate
                        exchangeRateP.ExchangeRate = rate_d; // Update the rate
                        currencyH.Add(exchangeRateP); // Add it to the collection
                        foundExchangeRate = true;
                        break;
                    }
                }
                if (foundExchangeRate == false) // If the currency exchange rate was not downloaded, we will new to create it from scratch
                {
                    TTUSAPI.DataObjects.CurrencyExchangeRateProfile exchangeRateP = new TTUSAPI.DataObjects.CurrencyExchangeRateProfile(); // Create empty Exchange Rate
                    exchangeRateP.BaseCurrency = m_lstCurrencies[currency1];
                    exchangeRateP.TermCurrency = m_lstCurrencies[currency2];
                    exchangeRateP.ExchangeRate = rate_d;  // Set the exchange rate
                    currencyH.Add(exchangeRateP);
                }
                ResultStatus res = m_TTUSAPI.UpdateCurrencyExchangeRates(currencyH);
                if (res.Result == ResultType.SentToServer) // On Success
                {
                    UpdateStatusBar("Currency exchange rate successfully updated.");
                }
                else if (res.Result == ResultType.ValuesUnchanged) // On No Change
                {
                    UpdateStatusBar("Currency exchange rate unchanged");
                }
                else //On Failure
                {
                    UpdateStatusBar("Failed to update currency exchange rates, " + res.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error updating the exchange rates: " + ex.Message);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutDTS aboutForm = new AboutDTS();
            aboutForm.ShowDialog(this);
        }


        private void btnConnect_Click(object sender, EventArgs e)
        {
            m_TTUSAPI.Login(txtUsername.Text, txtPassword.Text);
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
