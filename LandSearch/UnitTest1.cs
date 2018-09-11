using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using CsvHelper;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;
using Emgu.CV.Shape;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

namespace LandSearch
{
    [TestClass]
    public class UnitTest1
    {
        static IWebDriver driverFF;

        [AssemblyInitialize]
        public static void SetUp(TestContext context)
        {
            driverFF = new FirefoxDriver();
        }

        public class UnpaidTaxes
        {
            public string dueDate { get; set; }
            public string amount { get; set; }
        }

        public class TimberedAcreage
        {
            public string parcelID { get; set; }
            public string taxDescription { get; set; }
            public string grossAcres { get; set; }
            public string grossAccessedValue { get; set; }
            public string netTaxesDue { get; set; }
            public string owner { get; set; }
            public string ownerMailAddress { get; set; }
            public string ownerCityStateZip { get; set; }
            public string forested { get; set; }
            public string forestedPercent { get; set; }
            public string unpaideTax { get; set; }
            public string url { get; set; }
        }

        private bool IsElementPresent(By by)
        {
            try
            {
                driverFF.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        [TestMethod]
        public void FindTimberedAcreages()
        {

            List<TimberedAcreage> timberedAcreage = new List<TimberedAcreage>();
            List<string> reviewedParcels = new List<string>();
            double forestRate = 0.3; //forested percantage 30%
            double minAcreVal = 20;
            double maxAcreVal = 20.25;
            double step = 0.25;

            double minAcre = minAcreVal;
            double maxAcre = minAcre + step;

            int ct = 0;
            while (maxAcre <= maxAcreVal)
            {
                try
                {
                    driverFF.Navigate().GoToUrl("https://beacon.schneidercorp.com/Application.aspx?AppID=227&LayerID=3115&PageTypeID=2&PageID=1681&KeyValue=2514328001");

                    Thread.Sleep(4000);

                    if (IsElementPresent(By.XPath("//*[@id='appBody']/div[4]/div/div/div[3]/a[1]")))
                    {
                        driverFF.FindElement(By.XPath("//*[@id='appBody']/div[4]/div/div/div[3]/a[1]")).Click();
                    }

                    IWebElement checkUseAcreageBox = driverFF.FindElement(By.Id("ctlBodyPane_ctl02_ctl01_chkUseAcres"));
                    if (checkUseAcreageBox.Selected == false)
                        checkUseAcreageBox.Click();

                    IWebElement inputMinAcres = driverFF.FindElement(By.Id("ctlBodyPane_ctl02_ctl01_txtAcresLow"));
                    inputMinAcres.SendKeys(minAcre.ToString());

                    IWebElement inputMaxAcres = driverFF.FindElement(By.Id("ctlBodyPane_ctl02_ctl01_txtAcresHigh"));
                    inputMaxAcres.SendKeys(maxAcre.ToString());

                    IWebElement search = driverFF.FindElement(By.Id("ctlBodyPane_ctl02_ctl01_btnSearch"));
                    search.Click();
                    Thread.Sleep(500);

                    var searchResultsUrl = driverFF.Url;
                    IWebElement resultsTable = driverFF.FindElement(By.TagName("tbody"));
                    IList<IWebElement> resultRows = resultsTable.FindElements(By.TagName("tr"));

                    for (int iRow = 1; iRow < resultRows.Count; iRow++)
                    {
                        ct++;
                        if (ct > 10) break;
                        try
                        {
                            IWebElement rltTable = driverFF.FindElement(By.TagName("tbody"));
                            IList<IWebElement> rltRows = rltTable.FindElements(By.TagName("tr"));
                            IList<IWebElement> rows = rltRows[iRow].FindElements(By.TagName("td"));

                            IWebElement parcel = rows[1];
                              
                            if (reviewedParcels.Contains(parcel.Text))
                                break;

                            reviewedParcels.Add(parcel.Text);

                            TimberedAcreage property = new TimberedAcreage();
                            property.parcelID = parcel.Text;

                            IWebElement aBtn = rows[1].FindElement(By.TagName("a"));
                            aBtn.Click();
                            Thread.Sleep(1000);

                            property.taxDescription = driverFF.FindElement(By.Id("ctlBodyPane_ctl00_ctl01_lblLegalDescription")).Text;
                            property.grossAcres = driverFF.FindElement(By.Id("ctlBodyPane_ctl00_ctl01_lblGrossAcres")).Text;
                            property.owner = driverFF.FindElement(By.Id("ctlBodyPane_ctl01_ctl01_lstDeed_ctl01_lblDeedName_lnkSearch")).Text;
                            property.ownerMailAddress = driverFF.FindElement(By.Id("ctlBodyPane_ctl01_ctl01_lstDeed_ctl01_lnkAddress1")).Text;
                            property.ownerCityStateZip = driverFF.FindElement(By.Id("ctlBodyPane_ctl01_ctl01_lstDeed_ctl01_lblAddress3")).Text;
                            property.grossAccessedValue = driverFF.FindElement(By.XPath("//*[@id='ctlBodyPane_ctl10_ctl01_grdValuation']/tbody/tr[5]/td[3]")).Text;
                            property.netTaxesDue = driverFF.FindElement(By.XPath("//*[@id='ctlBodyPane_ctl11_ctl01_grdTaxation']/tbody/tr[15]/td[3]")).Text;

                            
                            IWebElement taxHistoryTable = driverFF.FindElement(By.Id("ctlBodyPane_ctl13_mSection"));
                            IList<IWebElement> historyRows = taxHistoryTable.FindElements(By.TagName("tr"));

                            property.unpaideTax = "FALSE";
                            for (int jRow = 1; jRow < historyRows.Count; jRow++)
                            {
                                IWebElement taxHistTable = driverFF.FindElement(By.Id("ctlBodyPane_ctl13_mSection"));
                                IList<IWebElement> histRows = taxHistTable.FindElements(By.TagName("tr"));
                                IList<IWebElement> cols = histRows[jRow].FindElements(By.TagName("td"));
                                IList<IWebElement> spans = cols[3].FindElements(By.TagName("span"));

                                string record = spans[0].Text;

                                if (record == "No")
                                {
                                    property.unpaideTax = "TRUE";
                                    break;
                                }

                                record = spans[1].Text;

                                if (record == "No")
                                {
                                    property.unpaideTax = "TRUE";
                                    break;
                                }
                            }

                            property.url = driverFF.Url;

                            IWebElement tabRow = driverFF.FindElement(By.Id("tabRow"));
                            System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> buttons = tabRow.FindElements(By.ClassName("tab-item-bar"));
                            buttons[7].Click();
                            Thread.Sleep(10000);


                            ITakesScreenshot ssdriver = driverFF as ITakesScreenshot;
                            Screenshot screenshot = ssdriver.GetScreenshot();

                            var bmpScreen = new Bitmap(new MemoryStream(screenshot.AsByteArray));
                            Image<Bgr, Byte> inpImage = new Image<Bgr, Byte>(bmpScreen);

                            double forestedPercent = getForestedPercent(inpImage);

                            if (forestedPercent >= forestRate)
                            {
                                property.forested = "TRUE";
                                property.forestedPercent = forestedPercent.ToString();
                                timberedAcreage.Add(property);
                            }

                        }
                        catch (Exception ex) { Console.Write(ex); };

                        driverFF.Navigate().GoToUrl(searchResultsUrl);
                        Thread.Sleep(1000);

                    }
                }

                catch (Exception ex) { Console.Write(ex); };

                minAcre = maxAcre + 0.01;
                maxAcre += step;
            }

            //write each property out to CSV
            var now = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
            string fileLocation = @"C:/Users/PHOENIX/Desktop/LandSearch/PolkCountyScrapes/ClaytonCountyAcreages" + now + ".csv";

            using (TextWriter textWriter = File.CreateText(fileLocation))
            {
                var csvWriter = new CsvWriter(textWriter);
                csvWriter.WriteRecords(timberedAcreage);
            }
        }

        public double getForestedPercent(Image<Bgr, Byte> img)
        {

            double percent = 0;

            int width = img.Width;
            int height = img.Height;

            Image<Bgr, Byte> rlt1 = img.Clone();
            rlt1.SetZero();

            for (int i = 0; i < width; i++)
            {
                
                for (int j = 0; j < height; j++)
                {
                    int b = img.Data[j, i, 0];
                    int g = img.Data[j, i, 1];
                    int r = img.Data[j, i, 2];

                    if (r == 0 || g == 0 || b == 206)
                    {
                        rlt1.Data[j, i, 0] = 255;
                        rlt1.Data[j, i, 1] = 255;
                        rlt1.Data[j, i, 2] = 255;
                    }
                }
            }

            UMat uimage = new UMat();
            CvInvoke.CvtColor(rlt1, uimage, ColorConversion.Rgb2Gray);

            UMat cannyEdges = new UMat();
            double dCannyThreLinking = 120.0;
            double dCannyThres = 180.0;

            CvInvoke.Canny(uimage, cannyEdges, dCannyThres, dCannyThreLinking);

            Image<Bgr, Byte> rlt2 = img.Clone();
            rlt2.SetZero();
            UMat mask = rlt2.ToUMat();

            Rectangle roi = new Rectangle();

            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                int count = contours.Size;
                for (int i = 0; i < count; i++)
                {
                    using (VectorOfPoint contour = contours[i])
                    {
                        if (CvInvoke.ContourArea(contour, false) > 10000) //only consider contour with area > 100 * 100
                        {
                            roi = CvInvoke.BoundingRectangle(contour);
                            CvInvoke.FillConvexPoly(mask, contour, new MCvScalar(255, 255, 255));
                            break;
                        }
                    }
                }
            }

            UMat result = new UMat();
            CvInvoke.BitwiseAnd(img, mask, result);

            Image<Bgr, Byte> buffer_im = result.ToImage<Bgr, Byte>();
            buffer_im.ROI = roi;

            Image<Bgr, Byte> cropped_im = buffer_im.Copy();
            Image<Bgr, Byte> cropped_src = cropped_im.Clone(); //for draw

            int nTotalPts = 0;
            int nForestedPts = 0;

            int minColor = 20;
            int MaxColor = 120;

            for (int i = 0; i < cropped_im.Width; i++)
                for (int j = 0; j < cropped_im.Height; j++)
                {
                    int b = cropped_im.Data[j, i, 0];
                    int g = cropped_im.Data[j, i, 1];
                    int r = cropped_im.Data[j, i, 2];

                    if (b + g + r > 1)
                    {
                        nTotalPts++;
                    }
                    if (b > minColor && b < MaxColor && g > minColor && g < MaxColor && r > minColor && r < MaxColor)
                    {
                        cropped_im.Data[j, i, 0] = 0;
                        cropped_im.Data[j, i, 1] = 0;
                        cropped_im.Data[j, i, 2] = 255;

                        nForestedPts++;
                    }
                }

            if (nForestedPts > 0)
            {
                percent = (double)nForestedPts / nTotalPts;

                //save cropped original images and forest detected images
                
                if (percent >= 0.3)
                {
                    var now = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
                    string fileLocation1 = @"C:/Users/PHOENIX/Desktop/LandSearch/CroppedImages/CroppedImages" + now + "_1.png";
                    string fileLocation2 = @"C:/Users/PHOENIX/Desktop/LandSearch/CroppedImages/CroppedImages" + now + "_2.png";
                    cropped_src.Save(fileLocation1);
                    cropped_im.Save(fileLocation2);
                }
                
            }

            return percent;
        }

    }
}