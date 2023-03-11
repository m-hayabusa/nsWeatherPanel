
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;

namespace nekomimiStudio.weatherPanel
{
    public class weatherPanel : UdonSharpBehaviour
    {
        public const string WEATHERPANEL_VERSION = "0.4.2";

        public VRCUrl url;

        public int location = 8;

        public Sprite sprite_Clear;
        public Sprite sprite_Cloud;
        public Sprite sprite_Rain;
        public Sprite sprite_Snow;
        public Sprite sprite_Thunder;
        public Sprite sprite_Rain_Windy;
        public Sprite sprite_Snow_Windy;

        private RectTransform loadingBar;
        private TextMeshProUGUI loadingText;
        private TextMeshProUGUI dateText;
        private TextMeshProUGUI popsText_pops;
        private TextMeshProUGUI popsText_date;
        private TextMeshProUGUI mapDate;
        private GameObject waitScreen;
        private GameObject updateNotify;

        private int lastlocation = 8;
        private string localUpdate, serverUpdate, originUpdate;
        private int updateIttr = 0;
        private int loaded = 0;
        private Parser parser;
        private DateTime time, localTime;

        void Start()
        {
            parser = this.GetComponent<Parser>();
            time = DateTime.Now;
            localTime = time.ToLocalTime();
            waitScreen = this.transform.Find("waitScreen").gameObject;
            updateNotify = this.transform.Find("Panel/Update").gameObject;
            loadingBar = this.transform.Find("bottombar/loadingBar").GetComponent<RectTransform>();
            loadingText = this.transform.Find("bottombar/loadingText").GetComponent<TextMeshProUGUI>();
            mapDate = this.transform.Find("Panel/Date/Text (TMP)").GetComponent<TextMeshProUGUI>();
            dateText = this.transform.Find("Panel/Time/Text (TMP)").GetComponent<TextMeshProUGUI>();
            popsText_date = this.transform.Find("Panel/Pops/date").GetComponent<TextMeshProUGUI>();
            popsText_pops = this.transform.Find("Panel/Pops/pops").GetComponent<TextMeshProUGUI>();
        }

        private bool isLoading = false;

        void Update()
        {
            updateIttr++;

            if (updateIttr % 5 == 0)
            {
                if ((isLoading || parser.isDone()) && waitScreen.activeSelf)
                {
                    waitScreen.SetActive(false);
                }

                if (loaded != parser.getCounter())
                {
                    if (parser.isDone())
                    {
                        if (parser.getString("version", 0, "ver") != WEATHERPANEL_VERSION)
                        {
                            updateNotify.SetActive(true);
                            updateNotify.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = $"v{WEATHERPANEL_VERSION} <color=#dc322f>(latest: v{parser.getString("version", 0, "ver")})</color>";
                        }

                        {
                            DateTime tmp;

                            DateTime.TryParse(parser.getString("meta", 0, "updated"), out tmp);
                            serverUpdate = tmp.ToLocalTime().ToString("yyyy/MM/dd HH:mm");

                            DateTime.TryParse(parser.getString("jma.go.jp/forecast", 0, "reportDatetime"), out tmp);
                            originUpdate = tmp.ToLocalTime().ToString("yyyy/MM/dd HH:mm");

                            dateText.text = $"現在時刻:<indent=50%>{localTime.ToString("yyyy/MM/dd HH:mm")}</indent><br>ローカル取得時刻:<indent=50%>{localUpdate}</indent><br>サーバ更新時刻:<indent=50%>{serverUpdate}</indent><br>気象庁更新時刻:<indent=50%>{originUpdate}</indent><br>";

                            renderMap();
                            updateMapView();
                        }

                        loadingText.text = "Reload";
                        loadingBar.sizeDelta = new Vector2(1188, loadingBar.sizeDelta.y);
                    }
                    loaded = parser.getCounter();
                }

                if (!parser.isDone() && isLoading)
                {
                    loadingText.text = "LOADING...";
                    progressbar(0);
                }
                else
                {
                    progressbar(1);
                }

                if (updateIttr > 1000)
                {
                    time = DateTime.Now;
                    localTime = time.ToLocalTime();
                    dateText.text = $"現在時刻:<indent=50%>{localTime.ToString("yyyy/MM/dd HH:mm")}</indent><br>ローカル取得時刻:<indent=50%>{localUpdate}</indent><br>サーバ更新時刻:<indent=50%>{serverUpdate}</indent><br>気象庁更新時刻:<indent=50%>{originUpdate}</indent><br>";
                    updateIttr = 0;
                }
            }
        }

        private int progressbar_animate_ittr = 0;
        private int progressbar_animate_delta = 2;

        private void progressbar(float prog)
        {
            if (prog <= 0.15)
            {
                loadingBar.localPosition = new Vector3(-594 + (progressbar_animate_ittr) * 11.88F, loadingBar.localPosition.y, loadingBar.localPosition.z);
                loadingBar.sizeDelta = new Vector2(198, loadingBar.sizeDelta.y);

                progressbar_animate_ittr += progressbar_animate_delta;

                if (progressbar_animate_ittr > 80 || progressbar_animate_ittr <= 0)
                    progressbar_animate_delta = -progressbar_animate_delta;

            }
            else
            {
                progressbar_animate_ittr = 0; progressbar_animate_delta = 2;
                loadingBar.localPosition = new Vector3(-594, loadingBar.localPosition.y, loadingBar.localPosition.z);
                loadingBar.sizeDelta = new Vector2((prog) * 1188, loadingBar.sizeDelta.y);
            }
        }

        private void weather_allday(Transform transform, Sprite a)
        {
            transform.Find("all").gameObject.SetActive(true);
            transform.Find("sometimes").gameObject.SetActive(false);
            transform.Find("then").gameObject.SetActive(false);

            transform.Find("all").Find("Image (2)").GetComponent<Image>().sprite = a;
        }

        private void weather_sometimes(Transform transform, Sprite a, Sprite b)
        {
            transform.Find("all").gameObject.SetActive(false);
            transform.Find("sometimes").gameObject.SetActive(true);
            transform.Find("then").gameObject.SetActive(false);

            transform.Find("sometimes").Find("Image").GetComponent<Image>().sprite = a;
            transform.Find("sometimes").Find("Image (1)").GetComponent<Image>().sprite = b;
        }

        private void weather_after(Transform transform, Sprite a, Sprite b)
        {
            transform.Find("all").gameObject.SetActive(false);
            transform.Find("sometimes").gameObject.SetActive(false);
            transform.Find("then").gameObject.SetActive(true);

            transform.Find("then").Find("Image").GetComponent<Image>().sprite = a;
            transform.Find("then").Find("Image (1)").GetComponent<Image>().sprite = b;
        }

        private void render_weather(Transform weather, string weatherCode)
        {
            switch (weatherCode)
            {
                case "100": //100: 晴
                case "124": //100: 晴山沿い雪
                case "123": //100: 晴山沿い雷雨
                case "131": //100: 晴明け方霧
                case "130": //100: 朝の内霧後晴
                    weather_allday(weather, sprite_Clear);
                    break;
                case "101": //101: 晴時々曇
                case "132": //101: 晴朝夕曇
                    weather_sometimes(weather, sprite_Clear, sprite_Cloud);
                    break;
                case "102": //102: 晴一時雨
                case "106": //102: 晴一時雨か雪
                case "108": //102: 晴一時雨か雷雨
                case "103": //102: 晴時々雨
                case "107": //102: 晴時々雨か雪
                case "140": //102: 晴時々雨で雷を伴う
                case "121": //102: 晴朝の内一時雨
                case "120": //102: 晴朝夕一時雨
                    weather_sometimes(weather, sprite_Clear, sprite_Rain);
                    break;
                case "104": //104: 晴一時雪
                case "160": //104: 晴一時雪か雨
                case "105": //104: 晴時々雪
                case "170": //104: 晴時々雪か雨
                    weather_sometimes(weather, sprite_Clear, sprite_Snow);
                    break;
                case "110": //110: 晴後時々曇
                case "111": //110: 晴後曇
                    weather_after(weather, sprite_Clear, sprite_Cloud);
                    break;
                case "125": //112: 晴午後は雷雨
                case "127": //112: 晴夕方から雨
                case "122": //112: 晴夕方一時雨
                case "128": //112: 晴夜は雨
                case "112": //112: 晴後一時雨
                case "113": //112: 晴後時々雨
                case "114": //112: 晴後雨
                case "118": //112: 晴後雨か雪
                case "119": //112: 晴後雨か雷雨
                case "126": //112: 晴昼頃から雨
                    weather_after(weather, sprite_Clear, sprite_Rain);
                    break;
                case "115": //115: 晴後一時雪
                case "116": //115: 晴後時々雪
                case "117": //115: 晴後雪
                case "181": //115: 晴後雪か雨
                    weather_after(weather, sprite_Clear, sprite_Snow);
                    break;
                case "200": //200: 曇
                case "231": //200: 曇海上海岸は霧か霧雨
                case "209": //200: 霧
                    weather_allday(weather, sprite_Cloud);
                    break;
                case "223": //201: 曇日中時々晴
                case "201": //201: 曇時々晴
                    weather_sometimes(weather, sprite_Cloud, sprite_Clear);
                    break;
                case "202": //202: 曇一時雨
                case "206": //202: 曇一時雨か雪
                case "208": //202: 曇一時雨か雷雨
                case "203": //202: 曇時々雨
                case "207": //202: 曇時々雨か雪
                case "240": //202: 曇時々雨で雷を伴う
                case "221": //202: 曇朝の内一時雨
                case "220": //202: 曇朝夕一時雨
                    weather_sometimes(weather, sprite_Cloud, sprite_Rain);
                    break;
                case "204": //204: 曇一時雪
                case "260": //204: 曇一時雪か雨
                case "205": //204: 曇時々雪
                case "270": //204: 曇時々雪か雨
                case "250": //204: 曇時々雪で雷を伴う
                    weather_sometimes(weather, sprite_Cloud, sprite_Snow);
                    break;
                case "210": //210: 曇後時々晴
                case "211": //210: 曇後晴
                    weather_after(weather, sprite_Cloud, sprite_Clear);
                    break;
                case "225": //212: 曇夕方から雨
                case "222": //212: 曇夕方一時雨
                case "226": //212: 曇夜は雨
                case "212": //212: 曇後一時雨
                case "213": //212: 曇後時々雨
                case "214": //212: 曇後雨
                case "218": //212: 曇後雨か雪
                case "219": //212: 曇後雨か雷雨
                case "224": //212: 曇昼頃から雨
                    weather_after(weather, sprite_Cloud, sprite_Rain);
                    break;
                case "229": //215: 曇夕方から雪
                case "230": //215: 曇夜は雪
                case "215": //215: 曇後一時雪
                case "216": //215: 曇後時々雪
                case "217": //215: 曇後雪
                case "281": //215: 曇後雪か雨
                case "228": //215: 曇昼頃から雪
                    weather_after(weather, sprite_Cloud, sprite_Snow);
                    break;
                case "306": //300: 大雨
                case "300": //300: 雨
                case "304": //300: 雨か雪
                case "350": //300: 雨で雷を伴う
                case "329": //300: 雨一時みぞれ
                case "328": //300: 雨一時強く降る
                    weather_allday(weather, sprite_Rain);
                    break;
                case "301": //301: 雨時々晴
                    weather_sometimes(weather, sprite_Rain, sprite_Clear);
                    break;
                case "302": //302: 雨時々止む
                    weather_sometimes(weather, sprite_Rain, sprite_Cloud);
                    break;
                case "309": //303: 雨一時雪
                case "303": //303: 雨時々雪
                case "322": //303: 雨朝晩一時雪
                    weather_sometimes(weather, sprite_Rain, sprite_Snow);
                    break;
                case "308": //308: 雨で暴風を伴う
                    weather_allday(weather, sprite_Rain_Windy);
                    break;
                case "320": //311: 朝の内雨後晴
                case "316": //311: 雨か雪後晴
                case "324": //311: 雨夕方から晴
                case "325": //311: 雨夜は晴
                case "311": //311: 雨後晴
                case "323": //311: 雨昼頃から晴
                    weather_after(weather, sprite_Rain, sprite_Clear);
                    break;
                case "321": //313: 朝の内雨後曇
                case "317": //313: 雨か雪後曇
                case "313": //313: 雨後曇
                    weather_after(weather, sprite_Rain, sprite_Cloud);
                    break;
                case "326": //314: 雨夕方から雪
                case "327": //314: 雨夜は雪
                case "314": //314: 雨後時々雪
                case "315": //314: 雨後雪
                    weather_after(weather, sprite_Rain, sprite_Snow);
                    break;
                case "405": //400: 大雪
                case "400": //400: 雪
                case "340": //400: 雪か雨
                case "450": //400: 雪で雷を伴う
                case "427": //400: 雪一時みぞれ
                case "425": //400: 雪一時強く降る
                case "426": //400: 雪後みぞれ
                    weather_allday(weather, sprite_Snow);
                    break;
                case "401": //401: 雪時々晴
                    weather_sometimes(weather, sprite_Snow, sprite_Clear);
                    break;
                case "402": //402: 雪時々止む
                    weather_sometimes(weather, sprite_Snow, sprite_Cloud);
                    break;
                case "409": //403: 雪一時雨
                case "403": //403: 雪時々雨
                    weather_sometimes(weather, sprite_Snow, sprite_Rain);
                    break;
                case "407": //406: 暴風雪
                case "406": //406: 風雪強い
                    weather_allday(weather, sprite_Snow_Windy);
                    break;
                case "420": //411: 朝の内雪後晴
                case "361": //411: 雪か雨後晴
                case "411": //411: 雪後晴
                    weather_after(weather, sprite_Snow, sprite_Clear);
                    break;
                case "421": //413: 朝の内雪後曇
                case "371": //413: 雪か雨後曇
                case "413": //413: 雪後曇
                    weather_after(weather, sprite_Snow, sprite_Cloud);
                    break;
                case "423": //414: 雪夕方から雨
                case "414": //414: 雪後雨
                case "422": //414: 雪昼頃から雨
                    weather_after(weather, sprite_Snow, sprite_Rain);
                    break;
            }
        }

        private void renderMap()
        {
            int day = 0;

            if (time.Hour > 16 || time.Hour < 5)
                day = 1;

            if (time.Hour > 16)
                mapDate.text = $"{time.AddDays(1).ToLocalTime().Day.ToString("d")}日の天気";
            else
                mapDate.text = $"{localTime.Day.ToString("d")}日の天気";

            for (int i = 0; i < parser.getLength("jma.go.jp/forecast"); i++)
            {
                string file_Daily = parser.getString("jma.go.jp/forecast", i, "filename_Forecast");

                Transform panel = this.transform.Find($"Panel/Map/MapPanel/Panel ({i})");
                if (panel != null)
                {
                    TextMeshProUGUI name = panel.Find("name").GetComponent<TextMeshProUGUI>();
                    name.text = parser.getString("jma.go.jp/forecast", i, "name");
                    TextMeshProUGUI temp = panel.Find("temp").GetComponent<TextMeshProUGUI>();
                    string minTemp = parser.getString(file_Daily, day, "mT");
                    string maxTemp = parser.getString(file_Daily, day, "MT");
                    string tmp = "";

                    if (minTemp != "")
                    {
                        tmp = $"<color=#268bd2>{minTemp.PadLeft(2, ' ')}</color>";
                    }
                    else
                    {
                        tmp = "--";
                    }

                    tmp += $" / <color=#dc322f>{maxTemp.PadLeft(2, ' ')}</color> ℃";

                    temp.text = tmp;

                    render_weather(panel.Find("weather"), parser.getString(file_Daily, 1, "wC"));
                }
            }
        }

        private void renderWeekly()
        {
            this.transform.Find($"Panel/Weekly/WeeklyPanel").gameObject.SetActive(true);

            string file_Weekly = parser.getString("jma.go.jp/forecast", location, "filename_Weekly");
            string file_Daily = parser.getString("jma.go.jp/forecast", location, "filename_Forecast");

            for (int i = 0; i < 2; i++)
            {
                Transform panel = this.transform.Find($"Panel/Weekly/WeeklyPanel/Panel ({i})");
                string min = parser.getString("jma.go.jp/forecast", i, "name");
                string temp = "<br>";
                string minTemp = parser.getString(file_Daily, i, "mT");
                string maxTemp = parser.getString(file_Daily, i, "MT");

                if (minTemp != "")
                {
                    temp += $"<color=#268bd2>{minTemp.PadLeft(2, ' ')}</color> ℃";
                }
                else
                {
                    temp += "-- ℃";
                }

                temp += "<br>";

                if (parser.getString(file_Daily, i, "MT") != "")
                {
                    temp += $"<color=#dc322f>{maxTemp.PadLeft(2, ' ')}</color> ℃";
                }
                else
                {
                    temp += "-- ℃";
                }

                panel.Find("temp").GetComponent<TextMeshProUGUI>().text = temp;
                panel.Find("rel").GetComponent<TextMeshProUGUI>().text = "";
                DateTime date;
                DateTime.TryParse(parser.getString(file_Daily, i, "date"), out date);
                panel.Find("date").GetComponent<TextMeshProUGUI>().text = date.ToString("M/d");
                panel.Find("weather_str").GetComponent<TextMeshProUGUI>().text = parser.getString(file_Daily, i, "w");
                render_weather(panel.Find("weather"), parser.getString(file_Daily, i, "wC"));
            }

            for (int i = 1; i < 7; i++)
            {
                Transform panel = this.transform.Find($"Panel/Weekly/WeeklyPanel/Panel ({i + 1})");
                string min = parser.getString("jma.go.jp/forecast", i, "name");
                panel.Find("temp").GetComponent<TextMeshProUGUI>().text = $"<color=#6c71c4>{parser.getString(file_Weekly, i, "pops").PadLeft(2, ' ')}</color> %<br><color=#268bd2>{parser.getString(file_Weekly, i, "mT").PadLeft(2, ' ')}</color> ℃<br><color=#dc322f>{parser.getString(file_Weekly, i, "MT").PadLeft(2, ' ')}</color> ℃";
                panel.Find("rel").GetComponent<TextMeshProUGUI>().text = parser.getString(file_Weekly, i, "rel").PadLeft(1, ' ');
                DateTime date;
                DateTime.TryParse(parser.getString(file_Weekly, i, "date"), out date);
                panel.Find("date").GetComponent<TextMeshProUGUI>().text = date.ToString("M/d");
                panel.Find("weather_str").GetComponent<TextMeshProUGUI>().text = parser.getString(file_Weekly, i, "w");
                render_weather(panel.Find("weather"), parser.getString(file_Weekly, i, "wC"));
            }
        }

        private void updateMapView()
        {
            renderWeekly();

            string file_DailyPops = parser.getString("jma.go.jp/forecast", location, "filename_RainPops");
            string popsStr = "", dateStr = "";
            for (int i = 0; i < parser.getLength(file_DailyPops); i++)
            {
                DateTime date;
                DateTime.TryParse(parser.getString(file_DailyPops, i, "date"), out date);
                popsStr += $"<color=#6c71c4>{parser.getString(file_DailyPops, i, "pops")}</color> %<br>";
                dateStr += $"{date.ToString("dd日 HH時")}<br>";
            }
            popsText_pops.text = popsStr;
            popsText_date.text = dateStr;

            Transform lastlocationPanel = this.transform.Find($"Panel/Map/MapPanel/Panel ({lastlocation})");
            Transform locationPanel = this.transform.Find($"Panel/Map/MapPanel/Panel ({location})");

            lastlocationPanel.GetComponent<Image>().color = new Color(0.02734375F, 0.2109375F, 0.2578125F, 0.9375F);
            lastlocationPanel.Find("name").GetComponent<TextMeshProUGUI>().color = new Color(0.98828125F, 0.9609375F, 0.88671875F, 1);

            locationPanel.GetComponent<Image>().color = new Color(0.98828125F, 0.9609375F, 0.88671875F, 0.9375F);
            locationPanel.Find("name").GetComponent<TextMeshProUGUI>().color = new Color(0.02734375F, 0.2109375F, 0.2578125F, 1);

            lastlocation = location;
        }

        public void reload() {
            VRCStringDownloader.LoadUrl(url, (VRC.Udon.Common.Interfaces.IUdonEventReceiver)this);
            isLoading = true;
        }
        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            parser.reset();
            parser.parse(result.Result);
            isLoading = false;
            localUpdate = DateTime.Now.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
        }
        public void onClick0() { location = 0; if (parser.isDone()) updateMapView(); }
        public void onClick1() { location = 1; if (parser.isDone()) updateMapView(); }
        public void onClick2() { location = 2; if (parser.isDone()) updateMapView(); }
        public void onClick3() { location = 3; if (parser.isDone()) updateMapView(); }
        public void onClick4() { location = 4; if (parser.isDone()) updateMapView(); }
        public void onClick5() { location = 5; if (parser.isDone()) updateMapView(); }
        public void onClick6() { location = 6; if (parser.isDone()) updateMapView(); }
        public void onClick7() { location = 7; if (parser.isDone()) updateMapView(); }
        public void onClick8() { location = 8; if (parser.isDone()) updateMapView(); }
        public void onClick9() { location = 9; if (parser.isDone()) updateMapView(); }
        public void onClick10() { location = 10; if (parser.isDone()) updateMapView(); }
        public void onClick11() { location = 11; if (parser.isDone()) updateMapView(); }
        public void onClick12() { location = 12; if (parser.isDone()) updateMapView(); }
        public void onClick13() { location = 13; if (parser.isDone()) updateMapView(); }
        public void onClick14() { location = 14; if (parser.isDone()) updateMapView(); }
        public void onClick15() { location = 15; if (parser.isDone()) updateMapView(); }
        public void onClick16() { location = 16; if (parser.isDone()) updateMapView(); }
        public void onClick17() { location = 17; if (parser.isDone()) updateMapView(); }
        public void onClick18() { location = 18; if (parser.isDone()) updateMapView(); }
        public void onClick19() { location = 19; if (parser.isDone()) updateMapView(); }
        public void onClick20() { location = 20; if (parser.isDone()) updateMapView(); }
        public void onClick21() { location = 21; if (parser.isDone()) updateMapView(); }
    }

}