# GHTMRM

General Hyper Text Message Respond Model.

其实是自用垃圾bot框架，不想学onebot所以手搓的一坨，慎用，别看代码怕丢脸。

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
  - 2: combine and forward

- supported cq code

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

## Session

### 记忆化

#### 编译器上下文

- 代码文本

#### 用户数据

- 提供两个dictionary，一个是sessionData一个是globalData，以json文本形式存储在本地，在session初始化时读入作为globals传入

## 插件系统



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

### 插件命令