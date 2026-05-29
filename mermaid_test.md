# Mermaid Test

## Test 1: Simple Flowchart (correct)

```mermaid
flowchart TD
    A[Start] --> B{Is it?}
    B -->|Yes| C[OK]
    B -->|No| D[End]
```

## Test 2: Sequence Diagram (correct)

```mermaid
sequenceDiagram
    A->>B: Hello
    B->>A: Hi there
```

## Test 3: Chinese labels with br (correct)

```mermaid
flowchart TD
    subgraph 模拟脑系统
        DTE[DTE<br/>输入编码]
        BrainCore[BrainCore<br/>核心协调]
        KLD[KLD<br/>输出解码]
        FeedbackLoop[FeedbackLoop<br/>反馈闭环]
        
        RewardActor[RewardActor<br/>奖励信号]
        DynamicTopology[DynamicTopology<br/>拓扑演化]
        EventScheduler[EventScheduler<br/>事件调度]
        MonitoringDashboard[MonitoringDashboard<br/>监控面板]
        
        CommProtocol[CommProtocol<br/>通信协议]
        ComputeBackend[ComputeBackend<br/>计算后端]
        ModelRepository[ModelRepository<br/>模型仓库]
    end

    DTE --> BrainCore
    BrainCore --> KLD
    FeedbackLoop -.-> DTE
    BrainCore --> DynamicTopology
    DynamicTopology --> EventScheduler
    EventScheduler -.-> BrainCore
    BrainCore --> RewardActor
    BrainCore --> MonitoringDashboard
    CommProtocol & ComputeBackend --> DynamicTopology
    BrainCore --> ModelRepository
    ModelRepository --> BrainCore
```

## Test 4: Full Chinese flowchart (correct)

```mermaid
sequenceDiagram
    participant T as 时间
    participant DTE
    participant ES as EventScheduler
    participant N as 神经元
    participant KLD
    participant FL as FeedbackLoop

    T->>DTE: t=0ms: "hello" 输入
    DTE->>ES: t=0ms: 生成 Spike 组 A
    ES->>N: t=5ms: 分发 Spike 到目标神经元
    N->>N: t=5ms: N1,N2,N3 处理输入，产生新 Spike
    N->>ES: t=8ms: 新 Spike 进入调度器
    DTE->>DTE: t=15ms: 完成编码，开始下一 token
    KLD->>N: t=30ms: 查询 Motor Layer 状态
    N-->>KLD: t=30ms: 产生输出 "world"
    KLD->>FL: t=30ms: 输出反馈
    FL-->>DTE: 注入反馈信号
```

## Test 5: Chinese flowchart full (correct)

```mermaid
flowchart TD
    subgraph 模拟脑系统
        DTE[DTE<br/>输入编码]
        BrainCore[BrainCore<br/>核心协调]
        KLD[KLD<br/>输出解码]
        FeedbackLoop[FeedbackLoop<br/>反馈闭环]
        
        RewardActor[RewardActor<br/>奖励信号]
        DynamicTopology[DynamicTopology<br/>拓扑演化]
        EventScheduler[EventScheduler<br/>事件调度]
        MonitoringDashboard[MonitoringDashboard<br/>监控面板]
        
        CommProtocol[CommProtocol<br/>通信协议]
        ComputeBackend[ComputeBackend<br/>计算后端]
        ModelRepository[ModelRepository<br/>模型仓库]
    end

    DTE --> BrainCore
    BrainCore --> KLD
    FeedbackLoop -.-> DTE
    BrainCore --> DynamicTopology
    DynamicTopology --> EventScheduler
    EventScheduler -.-> BrainCore
    BrainCore --> RewardActor
    BrainCore --> MonitoringDashboard
    CommProtocol & ComputeBackend --> DynamicTopology
    BrainCore --> ModelRepository
    ModelRepository --> BrainCore
```

## Test 6: Event sequence (correct)

```mermaid
sequenceDiagram
    participant DTE
    participant BrainCore
    participant Scheduler
    participant Neuron
    participant Synapse
    
    DTE->>BrainCore: encode()
    BrainCore->>Scheduler: schedule()
    Scheduler->>Scheduler: pop_next()
    Scheduler-->>BrainCore: 事件
    BrainCore->>Neuron: deliver()
    Neuron->>Neuron: handle_input()
    Neuron->>Neuron: fire()
    Neuron->>Scheduler: schedule()
    Neuron->>Synapse: handle_pre()
    BrainCore->>DTE: get_state()
    DTE->>DTE: decode()
```

End of tests.
