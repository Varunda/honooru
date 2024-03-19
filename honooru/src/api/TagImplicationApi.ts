import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

import { Tag, TagApi } from "api/TagApi";

export class TagImplication {
    public tagA: number = 0;
    public tagB: number = 0;
}

export class TagImplicationBlock {
    public tagID: number = 0;
    public sources: TagImplication[] = [];
    public targets: TagImplication[] = [];
    public tags: Tag[] = [];
}

export class TagImplicationApi extends ApiWrapper<TagImplication> {
    private static _instance: TagImplicationApi = new TagImplicationApi();
    public static get(): TagImplicationApi { return TagImplicationApi._instance; }

    public static parse(elem: any): TagImplication {
        return {
            ...elem
        };
    }

    public static parseBlock(elem: any): TagImplicationBlock {
        return {
            tagID: elem.tagID,
            sources: elem.sources.map((iter: any) => TagImplicationApi.parse(iter)),
            targets: elem.targets.map((iter: any) => TagImplicationApi.parse(iter)),
            tags: elem.tags.map((iter: any) => TagApi.parse(iter))
        };
    }

    public static getBySourceTagID(tagID: number): Promise<Loading<TagImplicationBlock>> {
        return TagImplicationApi.get().readSingle(`/api/tag-implication/${tagID}/block`, TagImplicationApi.parseBlock);
    }

    public static insert(tagA: number, tagB: number): Promise<Loading<void>> {
        return TagImplicationApi.get().post(`/api/tag-implication/?sourceTagID=${tagA}&targetTagID=${tagB}`);
    }

    public static delete(tagA: number, tagB: number): Promise<Loading<void>> {
        return TagImplicationApi.get().delete(`/api/tag-implication/?sourceTagID=${tagA}&targetTagID=${tagB}`);
    }

}
