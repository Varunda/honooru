<template>
    <div>
        <app-menu class="flex-grow-1">
            <menu-dropdown></menu-dropdown>
        </app-menu>

        <div style="display: grid; grid-template-columns: 400px 1fr; gap: 0.5rem; max-width: 100vw">
            <div>
                <div v-if="tags.state == 'idle'"></div>
                <div v-else-if="tags.state == 'loading'">
                    loading tags...
                </div>

                <div v-else-if="tags.state == 'loaded'">
                    <div v-for="block in sortedTags" class="mb-2" style="line-height: 1.2">
                        <h5 class="mb-0">
                            <strong>{{block.name}}</strong>
                        </h5>

                        <div v-for="tag in block.tags" :style="{ 'color': '#' + tag.hexColor }">
                            <info-hover :text="tag.description || '<no description given>'">
                            </info-hover>
                            <a :href="'/tag/' + tag.id">
                                <span :style="{ 'color': '#' + tag.hexColor }">
                                    {{tag.name}}
                                </span>
                            </a>
                            <span class="text-muted">
                                ({{tag.uses}})
                            </span>
                        </div>
                    </div>
                </div>

                <button class="btn btn-sm btn-primary">
                    edit
                </button>

                <hr class="border border-secondary" />

                <h5>misc controls</h5>

                <button class="btn btn-sm btn-secondary d-block mb-1" @click="remakeThumbnail">
                    remake thumbnail
                </button>

                <div class="border rounded border-danger">
                    <div class="bg-danger w-100 pb-1 mb-1 text-center">
                        dangerous operations
                    </div>

                    <button class="btn btn-sm btn-danger d-block mb-1 ml-1">
                        delete
                    </button>

                    <button class="btn btn-sm btn-danger d-block mb-1 ml-1">
                        !! erase !!
                    </button>
                </div>
            </div>

            <div v-if="post.state == 'loaded'">
                <file-view :md5="post.data.md5" :file-extension="post.data.fileExtension"></file-view>
            </div>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { AppMenu, MenuSep, MenuDropdown, MenuImage } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";

    import FileView from "components/app/FileView.vue";

    import { Post, PostApi } from "api/PostApi";
    import { ExtendedTag } from "api/TagApi";
    import { PostTagApi } from "api/PostTagApi";

    class TagBlock {
        public name: string = "";
        public hexColor: string = "";
        public tags: ExtendedTag[] = [];
    }

    export const ViewPost = Vue.extend({
        props: {

        },

        data: function() {
            return {
                postID: 0 as number,
                post: Loadable.idle() as Loading<Post>,

                tags: Loadable.idle() as Loading<ExtendedTag[]>
            }
        },

        mounted: function(): void {
            this.getPostID();
            this.loadAll();
        },

        methods: {
            getPostID: function(): void {
                const parts: string[] = location.pathname.split("/");
                if (parts.length < 3) {
                    console.error(`missing post ID`);
                    return;
                }

                this.postID = Number.parseInt(parts[2]);
            },

            loadAll: function(): void {
                this.loadPost();
                this.loadTags();
            },

            loadPost: async function(): Promise<void> {
                console.log(`loading post ${this.postID}`);
                this.post = Loadable.loading();
                this.post = await PostApi.getByID(this.postID);
            },

            loadTags: async function(): Promise<void> {
                console.log(`loading tags of post ${this.postID}`);
                this.tags = Loadable.loading();
                this.tags = await PostTagApi.getByPostID(this.postID);
            },

            remakeThumbnail: async function(): Promise<void> {
                if (this.post.state != "loaded") {
                    return console.warn(`cannot remake thumbnail: post is not loaded`);
                }

                await PostApi.remakeThumbnail(this.post.data.id);
            }
        },

        computed: {

            sortedTags: function(): TagBlock[] {
                if (this.tags.state != "loaded") {
                    return [];
                }

                const map: Map<number, TagBlock> = new Map();

                for (const tag of this.tags.data) {

                    let block: TagBlock | undefined = map.get(tag.typeID);

                    if (block == undefined) {
                        block = new TagBlock();
                        block.name = tag.typeName;
                        block.hexColor = tag.hexColor;
                        block.tags = [];
                    }

                    block.tags.push(tag);

                    map.set(tag.typeID, block);
                }

                const blocks: TagBlock[] = Array.from(map.values());

                for (const b of blocks) {
                    b.tags.sort((a, b) => {
                        return a.name.localeCompare(b.name);
                    });
                }

                return blocks;
            }

        },

        components: {
            InfoHover, ApiError,
            AppMenu, MenuSep, MenuDropdown, MenuImage,
            FileView
        }

    });
    export default ViewPost;

</script>