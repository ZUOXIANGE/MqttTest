using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;

namespace MqttClientWin
{
    public partial class FmMqttClient : Form
    {
        private IMqttClient mqttClient;

        public FmMqttClient()
        {
            InitializeComponent();
            Task.Run(async () => { await ConnectMqttServerAsync(); });
        }

        private async Task ConnectMqttServerAsync()
        {
            if (mqttClient == null)
            {
                mqttClient = new MqttFactory().CreateMqttClient();
                mqttClient.ApplicationMessageReceivedHandler =
                    new MqttApplicationMessageReceivedHandlerDelegate(MqttClient_ApplicationMessageReceived);
                mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(MqttClient_Connected);
                mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(MqttClient_Disconnected);
            }

            try
            {
                var options = new MqttClientOptions
                {
                    ClientId = Guid.NewGuid().ToString().Substring(0, 5),
                    CleanSession = true,
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "127.0.0.1",
                        Port = 61613
                    },
                    Credentials = new MqttClientCredentials
                    {
                        Username = "admin",
                        Password = Encoding.UTF8.GetBytes("123456")
                    }
                };
                await mqttClient.ConnectAsync(options);
            }
            catch (Exception ex)
            {
                Invoke(new Action(() =>
                {
                    txtReceiveMessage.AppendText("连接到MQTT服务器失败！" + Environment.NewLine + ex.Message +
                                                 Environment.NewLine);
                }));
            }
        }

        private void MqttClient_Disconnected(MqttClientDisconnectedEventArgs x)
        {
            Invoke(new Action(() => { txtReceiveMessage.AppendText("已断开MQTT连接！" + Environment.NewLine); }));
        }

        private void MqttClient_Connected(MqttClientConnectedEventArgs x)
        {
            Invoke(new Action(() => { txtReceiveMessage.AppendText("已连接到MQTT服务器！" + Environment.NewLine); }));
        }

        private void MqttClient_ApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs x)
        {
            Invoke(new Action(() =>
            {
                txtReceiveMessage.AppendText(
                    $">> {Encoding.UTF8.GetString(x.ApplicationMessage.Payload)}{Environment.NewLine}");
            }));
        }

        private void BtnSubscribe_Click(object sender, EventArgs e)
        {
            string topic = txtSubTopic.Text.Trim();

            if (string.IsNullOrEmpty(topic))
            {
                MessageBox.Show(@"订阅主题不能为空！");
                return;
            }

            if (!mqttClient.IsConnected)
            {
                MessageBox.Show(@"MQTT客户端尚未连接！");
                return;
            }

            var topicFilter = new TopicFilter
            {
                Topic = topic,
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            };
            mqttClient.SubscribeAsync(topicFilter);

            txtReceiveMessage.AppendText($"已订阅[{topic}]主题" + Environment.NewLine);
            txtSubTopic.Enabled = false;
            btnSubscribe.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string topic = txtPubTopic.Text.Trim();

            if (string.IsNullOrEmpty(topic))
            {
                MessageBox.Show(@"发布主题不能为空！");
                return;
            }

            string inputString = txtSendMessage.Text.Trim();
            var appMsg = new MqttApplicationMessageBuilder()
                .WithResponseTopic(topic)
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(inputString))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                .WithRetainFlag(false)
                .Build();
            mqttClient.PublishAsync(appMsg);
        }
    }
}