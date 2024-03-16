<template>
    <div>
        <app-menu class="flex-grow-1">
            <menu-dropdown></menu-dropdown>
        </app-menu>

        <hr class="border my-0" />

        <div style="display: grid; grid-template-columns: 400px 1fr; gap: 0.5rem; max-width: 100vw">
            <div>
                <div class="mb-2">
                    <label class="mb-0">rating</label>
                    <div class="btn-group w-100">
                        <button class="btn" :disabled="!editing"
                                :class="[ edit.rating == 'explicit' ? 'btn-primary' : 'btn-secondary' ]" @click="edit.rating = 'explicit'">
                            explicit
                        </button>
                        <button class="btn" :disabled="!editing" 
                                :class="[ edit.rating == 'unsafe' ? 'btn-primary' : 'btn-secondary' ]" @click="edit.rating = 'unsafe'">
                            unsafe
                        </button>
                        <button class="btn" :disabled="!editing"
                                :class="[ edit.rating == 'general' ? 'btn-primary' : 'btn-secondary' ]" @click="edit.rating = 'general'">
                            general
                        </button>
                    </div>
                </div>

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
                                <a :href="'/tag/' + tag.id">
                                    <info-hover :text="tag.description || ''"></info-hover>
                                </a>
                                <a :href="'/posts?q=' + tag.name">
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
                </div>

                <div v-if="editing == true" class="mb-1 border-top pt-2 mb-2">
                    <h5 class="mb-0">edit tags</h5>
                    <post-search ref="post-tags" v-model="edit.tags" type="textarea" @keyup.control.enter="saveEdit"></post-search>
                </div>

                <div class="mb-2">
                    <label class="mb-0 d-block">source</label>

                    <div v-if="editing == false">
                        <a v-if="edit.source != ''" :href="edit.source">{{edit.source}}</a>
                        <span v-else class="text-muted">
                            &lt;missing&gt;
                        </span>
                    </div>

                    <input v-else v-model="edit.source" class="form-control" />
                </div>

                <div v-if="editing == true" class="mb-2">
                    <label class="mb-0">title</label>
                    <input v-model="edit.title" class="form-control" />
                </div>

                <div v-if="editing == true" class="mb-2">
                    <label class="mb-0">description</label>
                    <textarea v-model="edit.description" class="form-control"></textarea>
                </div>

                <div>
                    <button v-if="editing == false" class="btn btn-sm btn-primary" @click="startEdit">
                        edit
                    </button>

                    <button v-if="editing == true" class="btn btn-sm btn-secondary" @click="cancelEdit">
                        cancel
                    </button>

                    <button v-if="editing == true" class="btn btn-sm btn-success" @click="saveEdit">
                        save
                    </button>
                </div>

                <hr class="border border-secondary" />

                <h5>misc controls</h5>

                <button class="btn btn-sm btn-secondary d-block mb-1" @click="remakeThumbnail">
                    remake thumbnail
                </button>

                <button class="btn btn-sm btn-danger d-block mb-1">
                    delete
                </button>

                <button class="btn btn-sm btn-danger d-block mb-1">
                    erase
                </button>
            </div>

            <div v-if="post.state == 'loaded'">
                <div class="h2">
                    <span v-if="post.data.title">
                        {{post.data.title}}
                    </span>

                    <span v-else class="text-muted">
                        &lt;no title set&gt;
                    </span>
                </div>

                <file-view :md5="post.data.md5" :file-extension="post.data.fileExtension"></file-view>

                <div>
                    <div class="h4">
                        Description
                    </div>
                    <div v-if="post.data.description">
                        {{post.data.description}}
                    </div>
                </div>
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
    import PostSearch from "components/app/PostSearch.vue";

    import { Post, PostApi } from "api/PostApi";
    import { ExtendedTag } from "api/TagApi";
    import { PostTagApi } from "api/PostTagApi";

    class TagBlock {
        public name: string = "";
        public hexColor: string = "";
        public tags: ExtendedTag[] = [];
    }

    const ratingIdMapping: Map<number, string> = new Map([
        [1, "general"],
        [2, "unsafe"],
        [3, "explicit"]
    ]);

    export const ViewPost = Vue.extend({
        props: {

        },

        data: function() {
            return {
                postID: 0 as number,
                post: Loadable.idle() as Loading<Post>,

                tags: Loadable.idle() as Loading<ExtendedTag[]>,

                edit: {
                    tags: "" as string,
                    title: "" as string,
                    description: "" as string,
                    source: "" as string,
                    rating: "" as string
                },

                editing: false as boolean
            }
        },

        mounted: function(): void {
            this.getPostID();
            this.loadAll();

            document.addEventListener("keyup", (ev: KeyboardEvent) => {
                if (ev.key == "e") {
                    this.startEdit();
                }

                if (ev.key == "Enter" && ev.ctrlKey) {
                    this.saveEdit();
                }

            });
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

                if (this.post.state == "loaded") {
                    this.edit.rating = ratingIdMapping.get(this.post.data.rating) ?? `<invalid ${this.post.data.rating}>`;
                    this.edit.title = this.post.data.title ?? "";
                    this.edit.description = this.post.data.description ?? "";
                    this.edit.source = this.post.data.source;
                }
            },

            loadTags: async function(): Promise<void> {
                console.log(`loading tags of post ${this.postID}`);
                this.tags = Loadable.loading();
                this.tags = await PostTagApi.getByPostID(this.postID);

                if (this.tags.state == "loaded") {
                    this.edit.tags = [...this.tags.data].sort((a, b) => a.name.localeCompare(b.name)).map(iter => iter.name).join(" ");
                }
            },

            startEdit: function(): void {
                this.editing = true;

                this.$nextTick(() => {
                    console.log(this.$refs);
                    (this.$refs["post-tags"] as any)?.focus();
                });
            },

            saveEdit: async function(): Promise<void> {
                await PostApi.update(this.postID, this.edit.tags, this.edit.rating, this.edit.source, this.edit.title, this.edit.description);

                this.loadAll();

                this.editing = false;
            },

            cancelEdit: function(): void {
                this.editing = false;

                if (this.post.state == "loaded") {
                    this.edit.rating = ratingIdMapping.get(this.post.data.rating) ?? `<invalid ${this.post.data.rating}>`;
                    this.edit.title = this.post.data.title ?? "";
                    this.edit.description = this.post.data.description ?? "";
                    this.edit.source = this.post.data.source;
                }

                if (this.tags.state == "loaded") {
                    this.edit.tags = [...this.tags.data].sort((a, b) => a.name.localeCompare(b.name)).map(iter => iter.name).join(" ");
                }
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
            FileView,
            PostSearch
        }

    });
    export default ViewPost;

</script>