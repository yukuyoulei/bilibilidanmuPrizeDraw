using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SimpleBilibiliDanmakuClient.Clients;
using SimpleBilibiliDanmakuClient.Models;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Live : MonoBehaviour
{
    public static int roomID = 0;

    public Queue<Danmaku> DanmakuQueue = new Queue<Danmaku>();
    public Queue<Gift> GiftQueue = new Queue<Gift>();
    public Queue<Guard> GuardQueue = new Queue<Guard>();
    public Queue<SuperChat> SCQueue = new Queue<SuperChat>();

    public Dictionary<int, string> imgdic;

    HttpRequestHelp h = new HttpRequestHelp();
    [Header("danmakuMinULLevel")] public static int danmakuMinULLevel = 0;

    public Text errorText;

    TcpDanmakuClientV2 client;
    List<TcpDanmakuClientV2> clients = new List<TcpDanmakuClientV2>();

    public float UILockTime = 5f;

    public void ReConnectToDanmu()
    {
        if (UILockTime > 0)
        {
            return;
        }

        UILockTime = 5f;
        Start();
    }

    async void Start()
    {
        if (roomID <= 0)
        {
            return;
        }

        try
        {
            imgdic = new Dictionary<int, string>();
            try
            {
                foreach (var client in clients)
                {
                    client.ReceivedMessageHandlerEvt -= Client_ReceivedMessageHandlerEvt;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            client = new TcpDanmakuClientV2();
            clients.Add(client);
            await client.ConnectAsync(roomID);

            client.ReceivedMessageHandlerEvt += Client_ReceivedMessageHandlerEvt;
            //client.ReceivedPopularityEvt += Client_ReceivedPopularityEvt;
            await Task.Delay(-1);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            errorText.text = e.ToString();
        }
    }

    private void Update()
    {
        UILockTime -= Time.deltaTime;
    }

    private void OnDestroy()
    {
        try
        {
            client.ReceivedMessageHandlerEvt -= Client_ReceivedMessageHandlerEvt;
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    #region ÈËÆø

    private Task Client_ReceivedPopularityEvt(IDanmakuClient client, ReceivedPopularityEventArgs e)
    {
        //Debug.Log("µ±Ç°Ö±²¥¼äÈËÆøÊý£º" + e.Popularity);
        //ListOfUserController.controller.SetPopularity(e.Popularity.ToString());
        return Task.CompletedTask;
    }

    #endregion


    private Task Client_ReceivedMessageHandlerEvt(IDanmakuClient client, ReceivedMessageEventArgs e)
    {
        try
        {
            string m = e.Message.ToString();
            //Debug.Log(m);
            JObject obj = (JObject)JsonConvert.DeserializeObject(m);
            string type = obj["cmd"].ToString();
            switch (type)
            {
                #region INTERACT_WORD

                case "INTERACT_WORD":

                    Int64 id = obj["data"]["uid"].ToObject<Int64>();
                    string name = obj["data"]["uname"].ToString();
                    byte msg_type = obj["data"]["msg_type"].ToObject<byte>();
                    //Debug.Log("msg_type"+ msg_type + "name" + name + "obj.ToString()" +obj.ToString());
                    break;

                #endregion

                #region WATCHED_CHANGE

                case "WATCHED_CHANGE":

                    int num = obj["data"]["num"].ToObject<int>();
                    //string text_small = obj["data"]["uname"].ToString();
                    //byte msg_type = obj["data"]["msg_type"].ToObject<byte>();
                    ListOfUserController.controller.SetPopularity(num.ToString());
                    break;

                #endregion

                #region DANMU_MSG

                case "DANMU_MSG":
                    Int64 userId = obj["info"][2][0].ToObject<Int64>();
                    string userName = obj["info"][2][1].ToString();
                    string content = obj["info"][1].ToString();
                    int mark = obj["info"][0][9].ToObject<int>();
                    string color = obj["info"][0][7].ToString();
                    byte guardLv = obj["info"][7].ToObject<byte>();
                    byte ulLv = obj["info"][4][0].ToObject<byte>();
                    string medelName = "";
                    byte medelLv = 0;
                    if (obj["info"][3].ToString().Length > 5)
                    {
                        medelName = obj["info"][3][1].ToString();
                        medelLv = obj["info"][3][0].ToObject<byte>();
                    }
                    
                                                                     
           
                    if (userName.Length == 4)
                    {
                        // 判断后三个字符是否为***
                        if (userName[1] == '*' && userName[2] == '*' && userName[3] == '*' )
                        {
                            Debug.LogError("chong lian");
                            ReConnectToDanmu();
                            return Task.CompletedTask;
                        }
                    }


                    if (guardLv > 0)
                    {
                        Debug.Log(string.Format(
                            "[userId{6}]{0} content:{1}   [guardLv:{2} medelName:{7}, medelLv:{3} ulLv:{4} color:{5}]",
                            userName, content, guardLv, medelLv, ulLv, color, userId, medelName));
                    }
                    
                    
                    string imgAddress = "";


                    //Debug.Log(string.Format("{0},{1}", obj["info"][3][1].ToString(), obj["info"][3][0].ToObject<byte>()));
                    if (ulLv >= danmakuMinULLevel)
                        DanmakuQueue.Enqueue(new Danmaku
                        {
                            name = userName,
                            text = content,
                            userID = userId,
                            GuardLv = guardLv,
                            color = color,
                            MedalLv = medelLv,
                            ULLv = ulLv,
                            MedalName = medelName,
                            imgAddress = imgAddress,
                        });
                    break;

                #endregion

                #region SEND_GIFT

                case "SEND_GIFT":
                    userId = obj["data"]["uid"].ToObject<Int64>();
                    userName = obj["data"]["uname"].ToString();
                    uint totalPrice = obj["data"]["total_coin"].ToObject<uint>();
                    string action = obj["data"]["action"].ToString();
                    string giftName = obj["data"]["giftName"].ToString();
                    uint giftCount = obj["data"]["num"].ToObject<uint>();
                    string coinType = obj["data"]["coin_type"].ToString();
                    //Debug.Log(string.Format("[ÀñÎïÐÅÏ¢]ÔùËÍÕß:{0}£¬ÀñÎïÃû£º{1} {2}¸ö£¬" +
                    //    "·½Ê½£º{3}£¬¼ÛÖµ£º{4}£¬ÀàÐÍ£º{5}", userName, giftName,
                    //    giftCount, action, totalPrice, coinType));

                    GiftQueue.Enqueue(new Gift
                    {
                        Action = action,
                        GiftName = giftName,
                        GiftCount = giftCount,
                        TotalPrice = totalPrice,
                        UserID = userId,
                        UserName = userName,
                        CoinType = coinType
                    });
                    break;

                #endregion

                #region USER_TOAST_MSG

                case "USER_TOAST_MSG": //½¢³¤
                    userId = obj["data"]["uid"].ToObject<Int64>();
                    userName = obj["data"]["username"].ToString();
                    giftName = obj["data"]["role_name"].ToString();
                    giftCount = obj["data"]["num"].ToObject<uint>();
                    byte guardLevel = obj["data"]["guard_level"].ToObject<byte>();
                    uint price = obj["data"]["price"].ToObject<uint>();
                    //Debug.Log(string.Format("[´óº½º£]ÔùËÍÕß£º{0}£¬ÀñÎïÃû£º{1} {2}¸ö£¬" +
                    //    "¼ÛÖµ£º{3}", userName, giftName, giftCount, price));

                    GuardQueue.Enqueue(new Guard
                    {
                        count = (int)giftCount,
                        GuardLevel = guardLevel,
                        GuardName = giftName,
                        userID = userId,
                        userName = userName,
                        Price = price
                    });
                    break;

                #endregion

                #region SUPER_CHAT_MESSAGE

                case "SUPER_CHAT_MESSAGE": //SuperChat
                    uint SCID = obj["data"]["id"].ToObject<uint>();
                    userId = obj["data"]["uid"].ToObject<Int64>();
                    price = obj["data"]["price"].ToObject<uint>();
                    string message = obj["data"]["message"].ToString();
                    userName = obj["data"]["user_info"]["uname"].ToString();
                    //Debug.Log(string.Format("[SuperChat]·¢ËÍÕß£º{0}£¬ÄÚÈÝ£º{1}£¬¼ÛÖµ£º{2}",
                    //    userName, message, price));

                    SCQueue.Enqueue(new SuperChat
                    {
                        Message = message,
                        price = (int)price,
                        ScID = SCID,
                        UserID = userId,
                        userName = userName
                    });
                    break;

                #endregion

                #region OTHER

                case "ONLINE_RANK_V2": //¸ßÄÜ°ñ
                    break;
                case "ROOM_BLOCK_MSG": //ºÚÃûµ¥
                    break;
                case "ROOM_REAL_TIME_MESSAGE_UPDATE": //·ÛË¿Êý¸üÐÂ
                    uint fans = obj["data"]["fans"].ToObject<uint>();
                    Debug.Log(string.Format("[·ÛË¿Êý·¢Éú±ä»¯]£º{0}", fans));
                    break;

                #endregion
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}