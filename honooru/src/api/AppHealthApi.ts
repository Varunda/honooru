import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class AppHealth {
    public queues: ServiceQueueCount[] = [];
    public timestamp: Date = new Date();
}

export class ServiceQueueCount {
    public queueName: string = "";
    public count: number = 0;
    public average: number | null = null;
    public min: number | null = null;
    public max: number | null = null;
    public median: number | null = null;
}

export class AppHealthApi extends ApiWrapper<AppHealth> {
    private static _instance: AppHealthApi = new AppHealthApi();
    public static get(): AppHealthApi { return AppHealthApi._instance; }

    public static parseQueue(elem: any): ServiceQueueCount {
        return {
            ...elem
        };
    }

    public static parse(elem: any): AppHealth {
        return {
            queues: elem.queues.map((iter: any) => AppHealthApi.parseQueue(iter)),
            timestamp: new Date(elem.timestamp)
        };
    }

    public static async getHealth(): Promise<Loading<AppHealth>> {
        return AppHealthApi.get().readSingle(`/api/health`, AppHealthApi.parse);
    }

}
