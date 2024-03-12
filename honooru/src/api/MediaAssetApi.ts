import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import * as axios from "axios";

export class MediaAsset {
    public guid: string = ""; // guid
    public md5: string = "";
    public status: number = 0;
    public fileName: string = "";
    public fileExtension: string = "";
    public timestamp: Date = new Date();
    public fileSizeBytes: number = 0;
}

export class MediaAssetApi extends ApiWrapper<MediaAsset> {
    private static _instance: MediaAssetApi = new MediaAssetApi();
    public static get(): MediaAssetApi { return MediaAssetApi._instance; }

    public static parse(elem: any): MediaAsset {
        return {
            timestamp: new Date(elem.timestamp),
            ...elem
        };
    }

    public static async upload(file: File, processCallback: (arg0: number, arg1: number) => void): Promise<Loading<MediaAsset>> {
        const formData: FormData = new FormData();
        formData.append("data", file);

        return MediaAssetApi.get().postReplyForm("/api/media-asset/upload", formData, MediaAssetApi.parse, {
            headers: {
                "Content-Type": "multipart/form-data"
            },
            onUploadProgress: (ev) => {
                processCallback(ev.loaded, ev.total);
            }
        });
    }

}
