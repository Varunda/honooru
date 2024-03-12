import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class Tag {

}

export class TagType {

}

export class ExtendedTag {
    public id: number = 0;
    public name: string = "";
    public typeID: number = 0;
    public typeName: string = "";
    public hexColor: string = "";
    public uses: number = 0;
    public description: string | null = null;
}

export class TagSearchResults {
    public input: string = "";
    public tags: ExtendedTag[] = [];
}

export class TagApi extends ApiWrapper<Tag> {

    private static _instance: TagApi = new TagApi();
    public static get(): TagApi { return TagApi._instance; }

    public static parseExtendedTag(elem: any): ExtendedTag {
        return {
            ...elem
        };
    }

    public static parseTagSearchResults(elem: any): TagSearchResults {
        return {
            input: elem.input,
            tags: elem.tags.map((iter: any) => TagApi.parseExtendedTag(iter))
        }
    }

    public static async search(name: string): Promise<Loading<TagSearchResults>> {
        return TagApi.get().readSingle(`/api/tag/search?name=${encodeURI(name)}`, TagApi.parseTagSearchResults);
    }

}