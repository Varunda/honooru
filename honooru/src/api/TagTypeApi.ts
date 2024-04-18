import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class TagType {
    public id: number = 0;
    public name: string = "";
    public hexColor: string = "";
    public alias: string = "";
    public order: number = 0;
    public darkText: boolean = false;
}

export class TagTypeApi extends ApiWrapper<TagType> {
    private static _instance: TagTypeApi = new TagTypeApi();
    public static get(): TagTypeApi { return TagTypeApi._instance; }

    public static parse(elem: any): TagType {
        return {
            ...elem
        };
    }

    public static getAll(): Promise<Loading<TagType[]>> {
        return TagTypeApi.get().readList(`/api/tag-type/`, TagTypeApi.parse);
    }

    public static insert(tagType: TagType): Promise<Loading<TagType>> {
        return TagTypeApi.get().postReplyForm(`/api/tag-type/`, tagType, TagTypeApi.parse);
    }

    public static update(typeID: number, tagType: TagType): Promise<Loading<TagType>> {
        return TagTypeApi.get().postReplyForm(`/api/tag-type/${typeID}`, tagType, TagTypeApi.parse);
    }

}
