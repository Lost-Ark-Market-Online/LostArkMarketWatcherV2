using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using OpenCvSharp;
using TesseractOCR.Enums;
using TesseractOCR;
using Rect = OpenCvSharp.Rect;
using Point = OpenCvSharp.Point;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Text.RegularExpressions;
using System.Text.Json;
using FuzzySharp;
using FuzzySharp.Extractor;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace LostArkMarketWatcherV2.modules
{
    public enum Rarity
    {
        COMMON,
        UNCOMMON,
        RARE,
        EPIC,
        LEGENDARY,
        RELIC,
        ANCIENT,
        SIDEREAL,
    }
    public class MarketLine
    {
        public Rarity Rarity;
        public string ItemId;
        public double AvgPrice;
        public double RecentPrice;
        public double LowestPrice;
        public int CheapestRemaining;

        public MarketLine(Rarity rarity, string itemId, double avgPrice, double recentPrice, double lowestPrice, int cheapestRemaining)
        {
            Rarity = rarity;
            ItemId = itemId;
            AvgPrice = avgPrice;
            RecentPrice = recentPrice;
            LowestPrice = lowestPrice;
            CheapestRemaining = cheapestRemaining;
        }
        public override string ToString()
        {
            return $"MarketLine<{this.Rarity}, {this.ItemId}, {this.AvgPrice}, {this.RecentPrice}, {this.LowestPrice}, {this.CheapestRemaining}>";
        }
    }

    class LamoScanMetadata
    {
        public readonly Vector2 baseRes;
        public readonly Rect goldFrame;
        public readonly List<List<Rect>> marketFrames;
        public readonly List<List<Rect>> interestFrames;
        public readonly Dictionary<string, List<List<Rect>>> frames;
        public readonly Dictionary<string, string> items;

        public readonly static LamoScanMetadata Instance = new();

        private LamoScanMetadata()
        {
            Rect[] marketRowFrames = [
                new Rect(630, 0, 500, 90),
                new Rect(1248, 0, 240, 90),
                new Rect(1590, 0, 220, 90),
                new Rect(1920, 0, 215, 90),
                new Rect(2320, 0, 330, 90),
            ];
            this.baseRes = new(3840, 2160);
            this.goldFrame = new Rect(1300, 210, 160, 70);
            Rect[] interestRowFrames = [
                new Rect(130, 0, 700, 90),
                new Rect(1230, 0, 240, 90),
                new Rect(1570, 0, 220, 90),
                new Rect(1900, 0, 215, 90),
                new Rect(2300, 0, 330, 90),
            ];
            this.marketFrames = [];
            this.interestFrames = [];

            for (int i = 0; i < 10; i++)
            {
                List<Rect> marketRow = [];
                List<Rect> interestRow = [];
                foreach (Rect col in marketRowFrames)
                {
                    Rect newCol = new(col.Location, col.Size)
                    {
                        Y = (i * 114) + 338
                    };
                    marketRow.Add(newCol);
                }
                foreach (Rect col in interestRowFrames)
                {
                    Rect newCol = new(col.Location, col.Size)
                    {
                        Y = (i * 114) + 338
                    };
                    interestRow.Append(newCol);
                }
                marketFrames.Add(marketRow);
                interestFrames.Append(interestRow);
            }
            this.frames = new Dictionary<string, List<List<Rect>>>
            {
                { "market", marketFrames },
                { "interest", interestFrames }
            };

            string json = File.ReadAllText("assets/data/items.json");
            this.items = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
        }
    }

    public class LamoScan
    {
        string screenshot_path;
        string detected_tab;
        Point detected_origin;
        Mat screenshot;
        Mat debugScreenshot;
        readonly LamoLogger logger;

        public async Task Scan()
        {
            await Task.Run(async () =>
            {
                try { 
                    this.logger.Info($"Scan: {this.screenshot_path}");
                    if (!Directory.Exists("debug"))
                    {
                        Directory.CreateDirectory("debug");
                    }
                    if (!Directory.Exists(Path.Join("debug", "inspection")))
                    {
                        Directory.CreateDirectory(Path.Join("debug", "inspection"));
                    }

                    // Crop Image
                    this.logger.Debug($"Read Screenshot");
                    Thread.Sleep(2000);
                    this.screenshot = Cv2.ImRead(this.screenshot_path);
                    this.logger.Debug($"Width:{this.screenshot.Width} | Height:{this.screenshot.Height}");
                    this.logger.Debug($"Crop Screenshot");
                    this.screenshot = CropScreenshot();
                    this.logger.Debug($"Resize Screenshot");
                    this.screenshot = ResizeScreenshot();
                    this.screenshot.CopyTo(this.debugScreenshot);
                    this.logger.Debug($"Detect Market");
                    DetectMarket();

                    List<MarketLine> results = [];
                    switch (this.detected_tab)
                    {
                        case "market":
                        case "interest":
                            this.logger.Debug($"Process Market");
                            results = ProcessMarketTableAsync();
                            break;
                        case "buy_crystals":
                        case "purchase_gold":
                            this.logger.Debug($"Process Crystal Table");
                            results = await ProcessCrystalTable();
                            break;
                    }
                    Cv2.ImWrite(Path.Join("debug", $"processed.png"), this.debugScreenshot);
                    Parallel.ForEach(results, new ParallelOptions { MaxDegreeOfParallelism = LamoConfig.Instance.upload_threads }, async result => {
                        try
                        {
                            await LamoApi.Upload(result);
                            logger?.Debug($"[Updated] {result.ItemId}");
                        }
                        catch (Exception e)
                        {
                            logger?.Error($"[Error] Updating {result.ItemId}");
                            logger?.Error(e.Message);
                        }
                    });

                }
                catch (Exception ex)
                {
                    this.logger.Error(ex.Message);
                    if (ex.StackTrace != null)
                    {
                        this.logger.Error(ex.StackTrace);
                    }
                }
            });
        }
        private MarketLine? ProcessLine(int line_index)
        {
            try
            {
                Task<dynamic>[] tasks = new Task<dynamic>[6];
                tasks[0] = this.GetRarity(line_index);
                tasks[0].Wait();
                Parallel.For(1, 6, new ParallelOptions { MaxDegreeOfParallelism = 2 }, i =>
                {
                    tasks[i] = this.ProcessLineColumn(line_index, i - 1);
                    tasks[i].Wait();
                });
                string scanName = tasks[1].Result;
                if (scanName == "")
                {
                    return null;
                }
                ExtractedResult<string> result = Process.ExtractOne(scanName, [.. LamoScanMetadata.Instance.items.Values]);
                string[] raritySuffixes = [
                    "(Common)",
                    "(Uncommon)",
                    "(Rare)",
                    "(Epic)",
                    "(Legendary)",
                    "(Relic)",
                    "(Ancient)",
                    "(Sidereal)",
                ];
                foreach (string suffix in raritySuffixes)
                {
                    if (result.Value.EndsWith(suffix))
                    {
                        scanName += $" {raritySuffixes[((int)tasks[0].Result)]}";
                        result = Process.ExtractOne(scanName, [.. LamoScanMetadata.Instance.items.Values]);
                        break;
                    }
                }
                string itemId = LamoScanMetadata.Instance.items.Keys.ToList()[LamoScanMetadata.Instance.items.Values.ToList().IndexOf(result.Value)];
                MarketLine marketLine = new MarketLine(
                    (Rarity)tasks[0].Result,
                    itemId,
                    (double)tasks[2].Result,
                    (double)tasks[3].Result,
                    (double)tasks[4].Result,
                    (int)tasks[5].Result
                );
                this.logger.Debug(marketLine.ToString());
                return marketLine;
            }
            catch (Exception e)
            {
                this.logger.Error(e.Message);
                this.logger.Error(e.StackTrace!);
                throw;
            }
        }
        private async Task<dynamic> GetRarity(int line_index)
        {
            try
            {
                Rect rect = new(
                    this.detected_origin.X + LamoScanMetadata.Instance.frames[this.detected_tab][line_index][0].X - 5,
                    this.detected_origin.Y + LamoScanMetadata.Instance.frames[this.detected_tab][line_index][0].Y + LamoScanMetadata.Instance.frames[this.detected_tab][line_index][0].Height - 5,
                    10,
                    10
                );
                Cv2.Rectangle(this.debugScreenshot, rect, new Scalar(255, 255, 255), 1);
                Mat rarityImg = this.screenshot[rect];
                Cv2.ImWrite(Path.Join("debug", "inspection", $"text-{rect.X}-{rect.Y}-9-rarity-sample.png"), rarityImg);

                Cv2.CvtColor(rarityImg, rarityImg, ColorConversionCodes.BGR2HSV);
                Mat[] splitRarityImg = Cv2.Split(rarityImg);
                double color_value = splitRarityImg[0].Mean().ToDouble();
                double saturation_value = splitRarityImg[1].Mean().ToDouble();

                Cv2.ImWrite(Path.Join("debug", "inspection", $"text-{rect.X + 5}-{rect.Y + 5}-9-rarity-hue.png"), splitRarityImg[0]);
                Cv2.ImWrite(Path.Join("debug", "inspection", $"text-{rect.X + 5}-{rect.Y + 5}-9-rarity-saturation.png"), splitRarityImg[1]);

                if (saturation_value < 50)
                {
                    return Rarity.COMMON;
                }
                else
                {
                    if (color_value < 15)
                    {
                        return Rarity.RELIC;
                    }
                    else if (color_value < 20)
                    {
                        return Rarity.LEGENDARY;
                    }
                    else if (color_value < 50)
                    {
                        return Rarity.UNCOMMON;
                    }
                    else if (color_value < 89)
                    {
                        return Rarity.SIDEREAL;
                    }
                    else if (color_value < 100)
                    {
                        return Rarity.RARE;
                    }
                    else if (color_value < 150)
                    {
                        return Rarity.EPIC;
                    }
                    else
                    {
                        return Rarity.COMMON;
                    }
                }
            }
            catch (Exception e)
            {
                this.logger.Error(e.Message);
                this.logger.Error(e.StackTrace!);
                throw;
            }
        }
        private Task<string> GetText(Rect rect, bool is_name = false)
        {
            return Task.Run(() =>
            {
                try
                {
                    Cv2.Rectangle(this.debugScreenshot, rect, new Scalar(0, 255, 255), 2);
                    Mat preparedImage = this.screenshot[rect];
                    Cv2.ImWrite(Path.Join("debug", "inspection", $"text-{rect.X}-{rect.Y}-1-cropped.png"), preparedImage);

                    Cv2.CvtColor(preparedImage, preparedImage, ColorConversionCodes.BGR2GRAY);
                    Cv2.ImWrite(Path.Join("debug", "inspection", $"text-{rect.X}-{rect.Y}-2-gray.png"), preparedImage);

                    Cv2.Resize(preparedImage, preparedImage, new OpenCvSharp.Size(preparedImage.Width * 3, preparedImage.Height * 3));
                    Cv2.ImWrite(Path.Join("debug", "inspection", $"text-{rect.X}-{rect.Y}-3-scaled.png"), preparedImage);

                    Cv2.AddWeighted(preparedImage, 1.65, preparedImage, 0, -120, preparedImage);
                    Cv2.ImWrite(Path.Join("debug", "inspection", $"text-{rect.X}-{rect.Y}-4-contrast.png"), preparedImage);

                    Mat coords = new Mat();
                    Cv2.FindNonZero(preparedImage, coords);
                    Rect bRect = Cv2.BoundingRect(coords);
                    if (bRect.Width == 0) {
                        return "";
                    }
                    preparedImage = preparedImage[bRect];
                    Cv2.CopyMakeBorder(preparedImage, preparedImage, 10, 10, 10, 10, BorderTypes.Constant);
                    Cv2.ImWrite(Path.Join("debug", "inspection", $"text-{rect.X}-{rect.Y}-5-isolated.png"), preparedImage);

                    Cv2.BitwiseNot(preparedImage, preparedImage);
                    Cv2.ImWrite(Path.Join("debug", "inspection", $"text-{rect.X}-{rect.Y}-6-flipped.png"), preparedImage);

                    Cv2.Threshold(preparedImage, preparedImage, 240, 255, ThresholdTypes.Binary);
                    Cv2.ImWrite(Path.Join("debug", "inspection", $"text-{rect.X}-{rect.Y}-7-filtered.png"), preparedImage);

                    Mat element = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
                    Cv2.Erode(preparedImage, preparedImage, element, null, 3);
                    Cv2.ImWrite(Path.Join("debug", "inspection", $"text-{rect.X}-{rect.Y}-8-sharpen.png"), preparedImage);

                    Engine engine = new(@"./lib/tessdata", Language.English, EngineMode.Default);
                    TesseractOCR.Pix.Image image = TesseractOCR.Pix.Image.LoadFromMemory(preparedImage.ToBytes());
                    Page page;
                    if (is_name)
                    {
                        page = engine.Process(image, PageSegMode.SingleBlock);
                    }
                    else
                    {
                        page = engine.Process(image, PageSegMode.RawLine);
                    }
                    string text = page.Text;
                    page.Dispose();
                    return text;
                }
                catch (Exception e)
                {
                    this.logger.Error(e.Message);
                    this.logger.Error(e.StackTrace!);
                    throw;
                }
            });
        }
        private async Task<dynamic> ProcessLineColumn(int line_index, int column_index)
        {
            try
            {
                Rect rect = new(
                    this.detected_origin.X + LamoScanMetadata.Instance.frames[this.detected_tab][line_index][column_index].X,
                    this.detected_origin.Y + LamoScanMetadata.Instance.frames[this.detected_tab][line_index][column_index].Y,
                    LamoScanMetadata.Instance.frames[this.detected_tab][line_index][column_index].Width,
                    LamoScanMetadata.Instance.frames[this.detected_tab][line_index][column_index].Height
                );

                switch (column_index)
                {
                    case 0:
                        string name = (await GetText(rect, true));
                        name = Regex.Replace(name, @"\n.*S[oa][il]d in bund[il]es.*", "");
                        name = Regex.Replace(name, @"\n.*Untradable upon.*", "");
                        name = name.Replace("\n", "");
                        return name;
                    case 1:
                    case 2:
                    case 3:
                        try
                        {
                            return double.Parse(await GetText(rect));
                        }
                        catch
                        {
                            return 0;
                        }
                    case 4:
                        try
                        {
                            return int.Parse(await GetText(rect));
                        }
                        catch
                        {
                            return 0;
                        }
                    default:
                        throw new Exception("Invalid Column");
                }
            }
            catch (Exception e)
            {
                this.logger.Error(e.Message);
                this.logger.Error(e.StackTrace!);
                throw;
            }
        }
        private List<MarketLine> ProcessMarketTableAsync()
        {
            try
            {
                List<MarketLine> results = new ();
                Parallel.For(0, 10, new ParallelOptions { MaxDegreeOfParallelism = LamoConfig.Instance.scan_threads }, i =>
                {
                    MarketLine? line = this.ProcessLine(i);
                    if (line != null)
                    {
                        results.Add(line);
                    }
                });

                return results;
            }
            catch (Exception e)
            {
                this.logger.Error(e.Message);
                this.logger.Error(e.StackTrace!);
                throw;
            }

        }
        private async Task<List<MarketLine>> ProcessCrystalTable()
        {
            Rect rect = new(
                this.detected_origin.X + LamoScanMetadata.Instance.goldFrame.X,
                this.detected_origin.Y + LamoScanMetadata.Instance.goldFrame.Y,
                LamoScanMetadata.Instance.goldFrame.Width,
                LamoScanMetadata.Instance.goldFrame.Height
            );
            string priceRaw = await GetText(rect);
            double price = (double)int.Parse(priceRaw.Replace(".", "").Replace(",", ""));
            string name = "";
            if (this.detected_tab == "purchase_gold")
            {
                price = Math.Round(price / 238, 2);
                name = "Royal Crystal";
            }
            else if (this.detected_tab == "buy_crystals")
            {
                price = Math.Round(price / 238, 2);
                name = "Blue Crystal";
            }
            return [new MarketLine(Rarity.COMMON, name, price, price, price, 1)];
        }
        private Mat ResizeScreenshot()
        {
            Vector2 scale = new()
            {
                X = this.screenshot.Width / LamoScanMetadata.Instance.baseRes.X,
                Y = this.screenshot.Height / LamoScanMetadata.Instance.baseRes.Y
            };
            OpenCvSharp.Size newSize = new OpenCvSharp.Size(
                (int)(this.screenshot.Width / scale.X),
                (int)(this.screenshot.Height / scale.Y)
            );
            Mat resized_sc = new();
            Cv2.Resize(this.screenshot, resized_sc, newSize);
            return resized_sc;
        }
        private Mat CropScreenshot()
        {
            int sc_middle = this.screenshot.Width / 2;
            this.logger.Debug($"Width:{this.screenshot.Width} | Height:{this.screenshot.Height}");
            Mat crop_detect = this.screenshot[new Rect(sc_middle, 0, 1, this.screenshot.Height)];
            Mat res = new();
            Cv2.CvtColor(crop_detect, res, ColorConversionCodes.BGR2GRAY);
            Cv2.AddWeighted(res, 1.8, res, 0, -20, res);
            Mat coords = new();
            Cv2.FindNonZero(res, coords);
            Rect rect = Cv2.BoundingRect(coords);
            Mat cropped_sc = this.screenshot[new Rect(0, 0, this.screenshot.Width, rect.Height)];
            Cv2.ImWrite(Path.Join("debug", "cropped.png"), cropped_sc);
            return cropped_sc;
        }
        private void DetectMarket()
        {
            List<(double, Point, string)> matches = [];
            matches.Add(MatchMarket("interest"));
            matches.Add(MatchMarket("market"));
            matches.Add(MatchMarket("buy_crystals"));
            matches.Add(MatchMarket("purchase_gold"));

            double maxVal = 0;
            Point foundPoint = new();
            string foundTab = "";
            foreach ((double val, Point point, string tab) match in matches)
            {
                if (match.val > maxVal)
                {
                    maxVal = match.val;
                    foundPoint = match.point;
                    foundTab = match.tab;
                }
            }
            this.detected_origin = foundPoint;
            this.detected_tab = foundTab;
            this.logger.Debug($"Detected Origin: {this.detected_origin}");
            this.logger.Debug($"Detected Tab: {this.detected_tab}");
        }
        private (double, Point, string) MatchMarket(string tab = "market")
        {
            string sample_file = tab switch
            {
                "market" => "assets/scan/search_market.jpg",
                "interest" => "assets/scan/interest_market.jpg",
                "purchase_gold" => "assets/scan/purchase_gold.jpg",
                "buy_crystals" => "assets/scan/buy_crystals.jpg",
                _ => throw new Exception("Invalid Tab"),
            };
            Mat sample = Cv2.ImRead(sample_file);
            Mat sampleHSV = new();
            Cv2.CvtColor(sample, sampleHSV, ColorConversionCodes.BGR2HSV);
            Cv2.Split(sampleHSV, out Mat[] sampleHSVSplitChannels);

            Mat screenshotHSV = new();
            Cv2.CvtColor(this.screenshot, screenshotHSV, ColorConversionCodes.BGR2HSV);
            Cv2.Split(screenshotHSV, out Mat[] screenshotHSVSplitChannels);

            Mat result = new();
            Cv2.MatchTemplate(screenshotHSVSplitChannels[2], sampleHSVSplitChannels[2], result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out Point maxLoc);

            return (maxVal, maxLoc, tab);
        }

        public LamoScan(string screenshot_path)
        {
            logger = (System.Windows.Application.Current as LamoWatcherApp)?.logger!;
            this.screenshot_path = screenshot_path;
            this.detected_tab = "market";
            this.detected_origin = new(0, 0);
            this.screenshot = new Mat();
            this.debugScreenshot = new Mat();
        }
    }
}

