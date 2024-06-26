﻿import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class AdminApi extends ApiWrapper<void> {
    private static _instance: AdminApi = new AdminApi();
    public static get(): AdminApi { return AdminApi._instance; }

    public static remakeAllThumbnails(): Promise<Loading<void>> {
        return AdminApi.get().post(`/api/admin/remake-all-thumbnails`);
    }

    public static remakeAllIqdbEntries(force: boolean = true): Promise<Loading<void>> {
        return AdminApi.get().post(`/api/admin/remake-all-iqdb-entries?force=${force}`);
    }

    public static cacheEvict(key: string): Promise<Loading<void>> {
        return AdminApi.get().post(`/api/admin/cache-evict?key=${key}`);
    }

    public static recountTags(): Promise<Loading<void>> {
        return AdminApi.get().post(`/api/admin/recount-tags`);
    }

    public static updateFileType(): Promise<Loading<void>> {
        return AdminApi.get().post(`/api/admin/update-file-types`);
    }

}
