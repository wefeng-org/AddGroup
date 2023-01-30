using MG.WeCode;
using MG.WeCode.WeClients;
using Newtonsoft.Json;
using Plugin;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;

namespace AddGroupPlugin
{
    public class Plugin : IPlugin
    {
        private AddGroupConfig config;
        /// <summary>
        /// 插件名称
        /// </summary>
        public string Name => "加群监控";
        /// <summary>
        /// 插件版本
        /// </summary>
        public string Version => "v0.0.1";
        /// <summary>
        /// 插件作者
        /// </summary>
        public string Author => "Byboy";
        /// <summary>
        /// 插件描述
        /// </summary>
        public string Description => "加群后触发";
        /// <summary>
        /// 无需设置,主动发消息时使用
        /// </summary>
        public string OriginId { get; set; }

        /// <summary>
        /// 插件初始化
        /// </summary>
        public void Initialize()
        {
            if (File.Exists("Plugins/加群监控.inf")) {
                config = JsonConvert.DeserializeObject<AddGroupConfig>(File.ReadAllText("Plugins/加群监控.inf"));
            } else {
                config = new();
                File.WriteAllText("Plugins/加群监控.inf",JsonConvert.SerializeObject(config));
            }
            //收到消息
            Events.GetReceiveMsg += Events_GetReceiveMsg;
        }

        private async Task Events_GetReceiveMsg(SuperWx.TLS_BFClent sender,List<WeChat.Pb.Entites.AddMsg> e)
        {
            Random r = new();
            foreach (var item in e) {
                if (!config.StartGroupUserName.Contains(item.FromUserName.String_t))
                    continue;
                var t = item.Content.String_t;
                if (item.MsgType == 10000 || item.MsgType == 10003 || item.MsgType == 10002 || item.MsgType == 37 || item.MsgType == 51) {
                    var content = Regex.Replace(t,@"^(.*?)\n<sysmsg","<sysmsg",RegexOptions.IgnoreCase);
                    var document = new XmlDocument() { InnerXml = content };
                    var sysmsg = (XmlElement)document.SelectSingleNode("sysmsg");
                    var t1 = sysmsg?.Attributes["type"].Value;
                    {
                        var template = document.SelectSingleNode("sysmsg/sysmsgtemplate/content_template/template");
                        if (template == null) {
                            break;
                        }
                        if (Regex.IsMatch(template.InnerText,"加入(了)*群(聊|組)")) {
                            var links = sysmsg.SelectSingleNode("sysmsgtemplate/content_template/link_list").SelectNodes("link");
                            var linkUsername = "";
                            var linkNickname = "";
                            var inviterUsername = "";
                            var inviterNickname = "";


                            foreach (XmlElement link in links) {
                                var attribute = link.GetAttribute("name");
                                if (attribute == "adder" || attribute == "names") {
                                    var elements = link.SelectSingleNode("memberlist").SelectNodes("member");
                                    foreach (XmlElement element in elements) {
                                        linkUsername = element.SelectSingleNode("username").InnerText;
                                        linkNickname = element.SelectSingleNode("nickname").InnerText;
                                    }
                                } else if (attribute == "username" || attribute == "from") {
                                    var elements = link.SelectSingleNode("memberlist").SelectNodes("member");
                                    inviterUsername = elements[0].SelectSingleNode("username").InnerText;
                                    inviterNickname = elements[0].SelectSingleNode("nickname").InnerText;
                                }
                            }
                            await WeClient.Messages.SendTextMsg(sender.WX.UserLogin.OriginalId,
                                new List<string> { item.FromUserName.String_t },
                                $"欢迎@{linkNickname}加入群聊,邀请你进群的是@{inviterNickname}",
                                linkUsername + "," + inviterUsername);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 插件设置
        /// </summary>
        public void Setting()
        {
            //使用文本打开配置文件
            Process.Start("notepad.exe","Plugins/加群监控.inf");
        }
        /// <summary>
        /// 插件卸载
        /// </summary>
        public void Terminate()
        {
            Events.GetReceiveMsg -= Events_GetReceiveMsg;
        }
    }
}
