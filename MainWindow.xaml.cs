using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Interactions;
using Path = System.IO.Path;

namespace AutoZhiHuiShu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Button _start;
        private readonly TextBox _console;
        private EdgeDriver driver;
        private Thread thread;
        private Thread listeningThread;

        private bool running = false;

        private bool checkCaptcha = false;

        public MainWindow()
        {
            InitializeComponent();
            _start = FindName("StartButton") as Button;
            _console = FindName("ConsoleTextBox") as TextBox;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (running)
            {
                new Thread(End).Start();
                return;
            }

            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "msedgedriver.exe")))
            {
                _start.Content = "Error!!!";
                _console.Text += "Can not found msedgedriver.exe, program running failed\n";
                return;
            }

            _start.Content = "Stop";

            _console.Text = "";
            _console.Text += "Running...\n";

            running = true;

            thread = new Thread(Start);
            thread.Start();
        }

        private void Start()
        {
            try
            {
                var service = EdgeDriverService.CreateDefaultService(Environment.CurrentDirectory, "msedgedriver.exe");

                service.HideCommandPromptWindow = true;

                driver = new EdgeDriver(service);

                Login();
            }
            catch (ThreadInterruptedException)
            {
            }
            catch (Exception e)
            {
                Dispatcher.Invoke(() => { _console.Text += "Exception catches, ending program...\n"; });
                Dispatcher.Invoke(() => { _console.Text += e.ToString(); });
                End();
            }
        }

        private void Login()
        {
            driver.Navigate().GoToUrl("https://passport.zhihuishu.com/");

            Dispatcher.Invoke(() => { _console.Text += "Wait for login...\n"; });

            while (!driver.Url.Contains("www.zhihuishu.com"))
            {
                // Wait
            }

            if (!driver.Url.Contains("onlineweb.zhihuishu.com"))
            {
                driver.Navigate().GoToUrl("https://onlineweb.zhihuishu.com/");
            }

            Dispatcher.Invoke(() => { _console.Text += "Wait for select which to study...\n"; });

            while (!driver.Url.Contains("videoStudy.html"))
            {
                // Wait
            }

            listeningThread = new Thread(ListeningQuestionDialog);
            listeningThread.Start();

            Logined();
        }

        private void Logined()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(500);
                    if (driver.FindElement(By.CssSelector(
                            "#app > div > div.el-dialog__wrapper.dialog-warn > div > div.el-dialog__footer > span > button")) ==
                        null
                        || driver.FindElement(By.CssSelector(
                                "#app > div > div.el-dialog__wrapper.dialog-warn")).GetAttribute("style")
                            .Equals("display: none;")) continue;
                    driver.FindElement(By.CssSelector(
                            "#app > div > div.el-dialog__wrapper.dialog-warn > div > div.el-dialog__footer > span > button"))
                        .Click();
                    break;
                }

                while (true)
                {
                    Thread.Sleep(500);
                    if (driver.FindElement(
                            By.CssSelector(
                                "#app > div > div:nth-child(7) > div.dialog-read > div.el-dialog__header > i")) == null
                        || driver.FindElement(By.CssSelector("#app > div > div:nth-child(7)")).GetAttribute("style")
                            .Equals("display: none;")) continue;
                    driver.FindElement(
                        By.CssSelector(
                            "#app > div > div:nth-child(7) > div.dialog-read > div.el-dialog__header > i")).Click();
                    break;
                }

                SelectLatestVideo();
            }
            catch (ElementClickInterceptedException)
            {
                while (!checkCaptcha)
                {
                    // Wait   
                }

                Logined();
            }
        }

        private void SelectLatestVideo()
        {
            Dispatcher.Invoke(() => { _console.Text += "Select lastest video...\n"; });
            var finds = from element in driver.FindElement(
                        By.CssSelector(
                            "#app > div > div.box-content > div.box-right > div.el-scrollbar > div.el-scrollbar__wrap > div")
                    ).FindElements(By.ClassName("list"))
                    .SelectMany(it => it.FindElements(By.ClassName("cataloguediv-c")))
                    .Where(it => it.FindElements(By.TagName("div")).Count != 0)
                select element;

            try
            {
                var find = finds.First(it =>
                    it.FindElement(By.TagName("div"))
                        .FindElements(By.TagName("div")).Count >= 0
                    && it.FindElement(By.TagName("div"))
                        .FindElements(By.TagName("b")).Count == 1);
                find.Click();

                Thread.Sleep(5000);

                RunVideo();
            }
            catch (InvalidOperationException)
            {
                Dispatcher.Invoke(() => { _console.Text += "Progress Finished.\n"; });
                End();
            }
        }

        private void RunVideo()
        {
            try
            {
                Dispatcher.Invoke(() => { _console.Text += "Run video...\n"; });
                new Actions(driver)
                    .MoveToElement(
                        driver.FindElement(By.XPath("/html/body/div[1]/div/div[2]/div[1]/div[2]/div/div/div[8]")))
                    .Click()
                    .Perform();
                Dispatcher.Invoke(() => { _console.Text += "Set faster play speed rate...\n"; });
                new Actions(driver)
                    .MoveToElement(
                        driver.FindElement(By.XPath("/html/body/div[1]/div/div[2]/div[1]/div[2]/div/div/div[8]"))
                    )
                    .MoveToElement(
                        driver.FindElement(By.ClassName("speedBox"))
                    )
                    .MoveToElement(
                        driver.FindElement(By.ClassName("speedTab15")))
                    .Click(driver.FindElement(By.ClassName("speedTab15")))
                    .Perform();
            }
            catch (ElementClickInterceptedException)
            {
                while (!checkCaptcha)
                {
                    // Wait   
                }

                RunVideo();
            }
        }

        private void ListeningQuestionDialog()
        {
            try
            {
                while (true)
                {
                    if (driver.FindElements(By.ClassName("yidun_popup")).Count != 0)
                    {
                        if (!checkCaptcha)
                        {
                            Dispatcher.Invoke(() => { _console.Text += "Captcha detected, please finish it first\n"; });
                        }

                        checkCaptcha = true;
                        continue;
                    }

                    checkCaptcha = false;
                    if (driver.FindElements(By.ClassName("dialog-test")).Count != 0)
                    {
                        // driver.FindElement(By.ClassName("controlsBar")).GetAttribute("style").Contains("display: none;")
                        Dispatcher.Invoke(() => { _console.Text += "Skipping test dialog...\n"; });
                        foreach (var element in driver.FindElements(By.ClassName("topic-item")))
                        {
                            element.Click();
                        }

                        driver.FindElement(By.ClassName("dialog-test")).FindElement(By.ClassName("dialog-footer"))
                            .Click();

                        Thread.Sleep(500);
                        RunVideo();
                    }

                    if (driver.FindElements(By.ClassName("current_play")).Count != 0
                        && driver.FindElement(By.ClassName("current_play")).FindElement(By.TagName("div"))
                            .FindElement(By.TagName("div")).FindElements(By.TagName("b")).Count == 2)
                    {
                        SelectLatestVideo();
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
            }
            catch (Exception e)
            {
                Dispatcher.Invoke(() => { _console.Text += "Exception catches, ending program...\n"; });
                Dispatcher.Invoke(() => { _console.Text += e.ToString(); });
                End();
            }
        }

        private void End()
        {
            try
            {
                Dispatcher.Invoke(() => { _console.Text += "End...\n"; });
                Dispatcher.Invoke(() => { _start.Content = "Start"; });

                running = false;

                listeningThread.Interrupt();
                thread.Interrupt();

                driver?.Quit();
            }
            catch (ThreadInterruptedException)
            {
            }
            catch (Exception e)
            {
                Dispatcher.Invoke(() => { _console.Text += "Exception catches, ending program...\n"; });
                Dispatcher.Invoke(() => { _console.Text += e.ToString(); });
            }
        }
    }
}