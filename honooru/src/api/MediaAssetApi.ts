import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import * as axios from "axios";

export class MediaAsset {
    public guid: string = ""; // guid
    public postID: number | null = null;
    public md5: string = "";
    public status: number = 0;
    public fileName: string = "";
    public fileExtension: string = "";
    public timestamp: Date = new Date();
    public fileSizeBytes: number = 0;
    public source: string = "";
    public additionalTags: string = "";
    public title: string = "";
    public description: string = "";
    public context: string = "";
    public iqdbHash: string | null = null;
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

    public static async uploadUrl(url: string): Promise<Loading<MediaAsset>> {
        return MediaAssetApi.get().postReply(`/api/media-asset/upload-url?url=${encodeURIComponent(url)}`, MediaAssetApi.parse);
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

    public static async uploadChunk(file: File, progessCallback: (arg0: number, arg1: number) => void): Promise<Loading<MediaAsset>> {
        const uploadId: Loading<string> = await MediaAssetApi.get().postReply(`/api/media-asset/upload/new`, (elem: any) => elem);
        if (uploadId.state != "loaded") {
            return Loadable.rewrap(uploadId);
        }

        // while numbers are double precision, the max safe int value is 9007199254740991, which would be a 8'000TB file, so it's fine lol
        let index: number = 0;

        const CHUNK_SIZE: number = 1024 * 1024 * 80; // cloudflare limits POST requests to 100MB (at free tier), so only upload 80MB chunks

        while (index < file.size) {
            console.log(`MediaAssetApi> slice ${index} ${index + CHUNK_SIZE}`);
            const slice: Blob = file.slice(index, index + CHUNK_SIZE, file.type);
            const sliceFile: File = new File([slice], file.name, {
                type: file.type
            });
            //debugger;

            const formData: FormData = new FormData();
            formData.append("data", sliceFile);

            const r: Loading<MediaAsset> = await MediaAssetApi.get().postReplyForm(`/api/media-asset/upload/part?uploadId=${uploadId.data}`, formData, MediaAssetApi.parse, {
                headers: {
                    "Content-Type": "multipart/form-data"
                },
                onUploadProgress: (ev) => {
                    //progessCallback(ev.loaded, ev.total);
                }
            });
            progessCallback(index, file.size);

            index += CHUNK_SIZE;
        }

        return MediaAssetApi.get().postReply(`/api/media-asset/upload/${uploadId.data}/done`, MediaAssetApi.parse);
    }

    public static regenerateIqdb(assetID: string): Promise<Loading<void>> {
        return MediaAssetApi.get().post(`/api/media-asset/${assetID}/regenerate-iqdb`);
    }

    public static remove(assetID: string): Promise<Loading<void>> {
        return MediaAssetApi.get().delete(`/api/media-asset/${assetID}`);
    }

}
