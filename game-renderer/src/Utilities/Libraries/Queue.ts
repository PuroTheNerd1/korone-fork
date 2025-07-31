import { Console, List } from "./CS.js";

export interface QueuePromise<T> {
    task: (port: number) => Promise<T>;
    resolve: (value: T | PromiseLike<T>) => void;
    reject: (reason?: any) => void;
}

export class Queue<T> {
    private readonly pendingTasks = new List<QueuePromise<T>>();
    private isProcessing = false;

    constructor(public readonly boxId: string, public readonly port: number) {}

    public async process(): Promise<void> {
        if (this.isProcessing) return;

        this.isProcessing = true;
        Console.Debug(`[Queue ${this.port}] Starting processing.`);

        while (!this.pendingTasks.Empty()) {
            const item = this.pendingTasks.First();
            this.pendingTasks.Remove(item);

            try {
                Console.Debug(`[Queue ${this.port}] Executing task...`);
                const start = Date.now();
                const result = await item.task(this.port);
                const duration = Date.now() - start;

                item.resolve(result);
                Console.Debug(`[Queue ${this.port}] Task completed in ${duration}ms`);
            } catch (error: any) {
                Console.Error(
                    `[Queue ${this.port}][Box ${this.boxId}] Task failed.\nMessage: ${error?.message}\nStack: ${error?.stack || error}`
                );
                item.reject(error);
            }
        }

        this.isProcessing = false;
        Console.Debug(`[Queue ${this.port}] Finished processing.`);
    }

    public add(task: QueuePromise<T>): void {
        this.pendingTasks.Add(task);
    }

    public get length(): number {
        return this.pendingTasks.Count();
    }
}

export class QueueBox<T> {
    private readonly queues: Queue<T>[] = [];

    constructor(public readonly boxId: string, ports: number[]) {
        this.queues = ports.map(port => new Queue<T>(boxId, port));
    }

    public async enqueue(task: (port: number) => Promise<T>): Promise<T> {
        const selectedQueue = this.selectLeastBusyQueue();
        Console.Debug(`[QueueBox ${this.boxId}] Enqueuing task to port ${selectedQueue.port}, pending: ${selectedQueue.length}`);

        return new Promise<T>((resolve, reject) => {
            selectedQueue.add({ task, resolve, reject });
            selectedQueue.process();
        });
    }

    private selectLeastBusyQueue(): Queue<T> {
        return this.queues.reduce((least, current) =>
            current.length < least.length ? current : least
        );
    }
}
