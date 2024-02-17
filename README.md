# OmniReply

自用垃圾bot框架，不想学`OneBot`所以手搓的一坨，用户权限极大，慎用，别看代码怕丢脸。

## 结构和运作

`聊天软件`<->...<->`bridge`<->`responder`

`OmniReply`本身并不提供与聊天软件协议，你需要使用如`mirai`、`itchat`等软件对接聊天软件，并实现一个`bridge`，按照如下的接口约定将收到的消息发送给`Responder`（那个C#项目）并把从`Responder`收到的消息发送给聊天服务，这两者之间使用`WebSocket`沟通。

对于`mirai`（QQ）和`itchat`（微信），本仓库已经给出了`bridge`。其中微信部分可以开箱即用，但是`mirai`需要用户手动配置好一个`mirai`再通过`yirimirai`的形式接入。

基本上，你只需要保证一个`bridge`能收到消息并把消息转发，然后再启动`Responder`即可。

在`Responder`中，每个聊天室都是一个`Session`，而`Session`内部是一个`CsSandBox`（使用`Roslyn`的`C# Script SandBox`（所以用户权限极大）），用户可以通过如下功能中的方式在这个`SandBox`中执行自己编写的或者通过插件预设的代码，并获取回复。

## 接口约定

### bridge->core

#### 频道命名: type0

```json
[
    "type": 0,
    "id": 
]
```

#### 聊天消息: type1

```json
[
    "type": 1,
    "sender": {
    	group_id: "",
    	user_id: ""
    },
	"content": {
        id: "..."
        parts: [
            {
                type: 0,
                data: ""
            },
            {
                type: 1,
                data: "xxx=="
            },
            {
            	type: 2,
                parts: [
                    {
                        type: 0,
                        data: ""
                    }
                    ...
                ]
            }
        ]
    }
]
```

- content.parts.type

  - 0: text
  - 1: img
  - 2: chat history

- supported cq code // not supported yet

  - ```
    [CQ:at,qq=10001000]
    ```

  - ```
    [CQ:reply,id=123456]
    ```

### core->bridge

#### 聊天消息: type1

```json
[
    "type": 1,
    "receiver": {
    	group_id: "",
    	user_id: ""
    },
	"content": {
        parts: [
            {
                type: 0,
                data: ""
            },
            {
                type: 1,
                data: "xxx=="
            },
            {
            	type: 2,
                parts: [
                    {
                        type: 0,
                        data: ""
                    }
                    ...
                ]
            }
        ]
    }
]
```

## 插件系统

### 基本介绍和示例

在`files/plugins`下创建文件夹，文件夹字典序即为插件加载顺序。

文件夹内含有文件`code.cs`和`config.json`，其中`config.json`示例如下：

```json
{
    "name": "chatgpt",
    "using_namespaces": [
        "System.Net.Http"
    ],
    "using_static_classes": [],
    "references": [
    	"OmniReply.MessageObjects"
    ],
    "regexps": [
        {
            "regex": "^ai[\\s\\S]*",
            "function": "ChatGPT"
        }
    ]
}
```

在`session`初始化时，其`CsSandBox`自动加载所有没有被禁用的插件的`references`和`usings_namespaces`，其中`SandBox`的初始化代码即为所有开启的插件的`code.cs`和`using_static_classes`拼接而成。

当收到不以`#`和`$`开头的“插件命令时”，`Responder`按照插件加载顺序遍历所有插件的`regexps`，如果整段文本都匹配成功，则将这段消息的消息对象`OmniReply.MessageObjects`传入对应的`function`，并回复其传回的对象。若传回对象为`List<MessageParts>`，则直接发送该对象，否则发送对象的`.ToString()`

对于上述示例`config.json`，示例的`code.cs`如下

```csharp
string ChatGPT(ReceivedChatMessage s)
{
    var res = string.Empty;

    var url = "http://url/v1/chat/completions";
    var apiKey = "token here";

    var client = new HttpClient();
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

    List<Dictionary<string, object>> messages = new List<Dictionary<string, object>>();

    Dictionary<string, object> systemMessage = new Dictionary<string, object>
    {
        { "role", "system" },
        { "content", "Hello! How can I assist you today?" }
    };
    messages.Add(systemMessage);

    Dictionary<string, object> userMessage = new Dictionary<string, object>
    {
        { "role", "user" },
        { "content", s.Content.ToString()[2..] }
    };
    messages.Add(userMessage);

    Dictionary<string, object> rootObject = new Dictionary<string, object>
    {
        { "model", "gpt-4" },
        { "messages", messages }
    };

    string requestBody = JsonConvert.SerializeObject(rootObject, Formatting.Indented);

    var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
    var response = client.PostAsync(url, content).GetAwaiter().GetResult();
    var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

    try
    {
        if(responseContent == null || responseContent == string.Empty)
            return "ChatGPT has nothing to say.";

        var responseObj = JsonConvert.DeserializeObject<dynamic>(responseContent);

        if(responseObj == null)
        {
            return "ChatGPT has nothing to say.";
        }

        var choices = responseObj.choices;
        var choice = choices[0];

        res = choice.message.content;
    }
    catch (Exception e)
    {
        res = $"An error occurred while processing the request: {e.Message}";
    }

    return res;
}
```

### 数据存储

提供两个`dictionary`，一个是`sessionData`一个是`globalData`，以Json文本形式存储在本地，在`session`初始化时读入作为`globals`传入。

可以使用函数`GetSessionData()`、`GetGlobalData()`、`SetSessionData()`、`SetGlobalData()`进行读写，文本长度有一定限制。

## 功能

### `#`管理员命令

- session
  - list
  - reload
  - reset
- plugman
  - list
  - reload
    - all
    - plugin name
  - disable
  - enable

### `$`用户命令

将作为C#代码直接在`CsSandBox`中运行并发送结果。

### 插件命令

如上插件系统所述。