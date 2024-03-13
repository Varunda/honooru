﻿import { Loading, Loadable } from "Loading";
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
            ...elem,
            timestamp: new Date(elem.timestamp)
        };
    }

    public static async getByID(guid: string): Promise<Loading<MediaAsset>> {
        return MediaAssetApi.get().readSingle(`/api/media-asset/${guid}`, MediaAssetApi.parse);
    }

    public static async getProcessing(): Promise<Loading<MediaAsset[]>> {
        return MediaAssetApi.get().readList(`/api/media-asset/processing`, MediaAssetApi.parse);
    }

    public static async getReady(): Promise<Loading<MediaAsset[]>> {
        return MediaAssetApi.get().readList(`/api/media-asset/ready`, MediaAssetApi.parse);
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
