﻿<template>
    <div style="display: grid; grid-template-rows: min-content 1fr; gap: 0.5rem; max-height: 100vh; max-width: 100vw;">

        <app-menu></app-menu>

        <div style="display: grid; grid-template-columns: 400px 1fr; gap: 0.5rem; overflow: hidden">
            <div class="overflow-y-auto">
                <h2 class="mb-2 text-center pb-2 border-bottom">
                    post {{postID}}
                </h2>

                <div class="mb-2">
                    <label class="mb-0">rating</label>
                    <div class="btn-group w-100">
                        <button class="btn" :disabled="!editing"
                                :class="[ edit.rating == 'explicit' ? 'btn-primary' : 'btn-secondary' ]" @click="edit.rating = 'explicit'">
                            explict
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

                <div class="honooru-tags">
                    <div v-if="tags.state == 'idle'"></div>
                    <div v-else-if="tags.state == 'loading'">
                        loading tags...
                    </div>

                    <div v-else-if="tags.state == 'loaded'">
                        <div v-for="block in sortedTags" class="mb-3 no-underline-links" style="line-height: 1.2">
                            <h6 class="mb-1 px-2 py-1 rounded" style="font-size: 1rem;" :style="{ 'background-color': '#' + block.hexColor }">
                                <strong>{{block.name}}</strong>
                            </h6>

                            <div v-for="tag in block.tags" :style="{ 'color': '#' + tag.hexColor }">
                                <a :href="'/tag/' + tag.id" :class="{ 'text-success': tag.description, 'text-secondary': !tag.description }">
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

                <h5>information</h5>

                <div v-if="post.state == 'idle'"></div>

                <div v-else-if="post.state == 'loading'">
                    loading...
                </div>

                <table v-else-if="post.state == 'loaded'" class="table table-sm">
                    <tbody>
                        <tr>
                            <td>ID</td>
                            <td>{{post.data.id}}</td>
                        </tr>

                        <tr>
                            <td>timestamp</td>
                            <td>{{post.data.timestamp | moment}}</td>
                        </tr>

                        <tr>
                            <td>md5</td>
                            <td>{{post.data.md5}}</td>
                        </tr>

                        <tr>
                            <td>file size</td>
                            <td>{{post.data.fileSizeBytes | bytes}}</td>
                        </tr>

                        <tr>
                            <td>file extension</td>
                            <td>{{post.data.fileExtension}}</td>
                        </tr>

                        <tr>
                            <td>dimensions</td>
                            <td>{{post.data.width}}x{{post.data.height}}</td>
                        </tr>

                        <tr v-if="post.data.durationSeconds > 0">
                            <td>duration</td>
                            <td>{{post.data.durationSeconds | mduration}}</td>
                        </tr>
                    </tbody>
                </table>

                <api-error v-else-if="post.state == 'error'" :error="post.problem"></api-error>

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

            <div class="overflow-y-auto">
                <div v-if="post.state == 'idle'"></div>

                <div v-else-if="post.state == 'loading'">
                    loading...
                </div>

                <div v-else-if="post.state == 'nocontent'">
                    failed to find post {{postID}}
                </div>

                <div v-else-if="post.state == 'loaded'">
                    <file-view :md5="post.data.md5" :file-extension="post.data.fileExtension"></file-view>

                    <div class="h2 mt-2 pt-2">
                        <span v-if="post.data.title">
                            {{post.data.title}}
                        </span>

                        <span v-else class="text-muted">
                            &lt;no title set&gt;
                        </span>
                    </div>

                    <div v-if="post.data.description" class="border-top mt-2 pt-2">
                        <div v-if="htmlDesc.state == 'idle'"></div>

                        <div v-else-if="htmlDesc.state == 'loading'">
                            loading...
                        </div>

                        <div v-else-if="htmlDesc.state == 'loaded'" v-html="htmlDesc.data"></div>

                        <div v-else-if="htmlDesc.state == 'error'">
                            error loading description:
                            <api-error :error="htmlDesc.problem"></api-error>
                        </div>

                        <div v-else>
                            unchecked state of htmlDesc: {{htmlDesc.state}}
                        </div>

                    </div>
                </div>

                <api-error v-else-if="post.state == 'error'" :error="post.problem"></api-error>

                <div v-else>
                    unchecked state of post: {{post.state}}
                </div>
            </div>

        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { AppMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";

    import FileView from "components/app/FileView.vue";
    import PostSearch from "components/app/PostSearch.vue";

    import { Post, PostApi } from "api/PostApi";
    import { ExtendedTag, TagApi } from "api/TagApi";
    import { PostTagApi } from "api/PostTagApi";

    import "filters/ByteFilter";
    import "filters/MomentFilter";

    import { marked, MarkedExtension, Token, TokenizerAndRendererExtension } from "marked";
    import * as DOMPurify from "dompurify";

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

    const DEBUG: boolean = false;

    const _debug = (str: string): void => {
        if (DEBUG) {
            console.log(str);
        }
    }

    const rule = /^post #(\d+)/;
    const tagRule = /^\[\[(.+)\]\]/;

    const honooruMarkdownExtension: MarkedExtension = {
        // walks the tokens and turns a honooru-tag-link into an <a> with the correct ID
        walkTokens: async (token) => {
            _debug(`${token.type} :: ${token.raw}`);

            if (token.type != "honooru-tag-link") {
                return;
            }

            const tagName: string = token.tagName;
            _debug(`getting tag [tagName='${tagName}']`);

            const tag: Loading<ExtendedTag> = await TagApi.getByName(tagName);

            _debug(`loaded tag [state=${tag.state}]`);
            if (tag.state == "loaded") {
                token.tagData = tag.data;
            } else if (tag.state == "nocontent") {
                token.tagData = {
                    id: -1,
                    name: `NOT_FOUND:${tagName}`,
                    typeID: -1,
                    typeName: "invalid",
                    hexColor: "ff0000",
                    uses: -1,
                    description: ""
                };
            }
        },

        // async lets walkTokens above be async, which lets us get the tag data when we find a honooru-tag-link
        async: true,

        extensions: [
            // extension to turn post numbers into links
            // example:
            //          post #4
            //      would turn into
            //          <a href="/post/4">post 4</a>
            //
            {
                name: "honooru-post-link",
                start: (src: string) => src.indexOf("post #"),
                level: "inline",
                tokenizer: function(src: string, tokens: Token[]) {
                    _debug(`SRC "${src}"`);

                    const match = rule.exec(src);
                    if (match) {
                        const token = {
                            type: "link",
                            raw: match[0],
                            text: match[0].trim(),
                            href: `/post/${match[1]}`,
                            tokens: [{
                                type: "text",
                                raw: match[0],
                                text: match[0]
                            }]
                        }

                        return token;
                    }

                    return undefined;
                },
            },

            // extension to turn tags into links
            // example:
            //          [[tag]]
            //      would turn into
            //          <a href="/post/1">tag</a>
            // with the correct coloring to match the tag type, and the correct tag ID that matches the name
            {
                name: "honooru-tag-link",
                start: (src: string) => src.indexOf("[["),
                level: "inline",
                tokenizer: function(src: string, tokens: Token[]) {
                    const match = tagRule.exec(src);

                    if (!match) {
                        return undefined;
                    }

                    const token = {
                        type: "honooru-tag-link",
                        raw: match[0],
                        text: match[1].trim(),
                        tagName: match[1].trim(), // used in walkTokens
                        tokens: [{
                            type: "text",
                            raw: match[0],
                            text: match[1]
                        }]
                    };

                    return token;
                },

                // called after walkTokens has populated the data we want
                renderer: (token) => {
                    _debug(`renderer for tag link: ${token.tagData}`);
                    return `<a href="/tag/${token.tagData.id}" style="color: #${token.tagData.hexColor}">${token.tagData.name}</a>`;
                }
            }
        ],

        tokenizer: null,
    };

    ///////////////////////////////////////////////////
    // start vue component
    ///////////////////////////////////////////////////

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

                editing: false as boolean,

                htmlDesc: Loadable.idle() as Loading<string>,
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

                    marked.use(honooruMarkdownExtension);

                    this.htmlDesc = Loadable.loading();
                    const html: string | Promise<string> = marked.parse(this.post.data.description ?? "");

                    try {
                        if (typeof html == "string") {
                            this.htmlDesc = Loadable.loaded(DOMPurify.sanitize(html));
                        } else {
                            html.then((result: string) => {
                                this.htmlDesc = Loadable.loaded(DOMPurify.sanitize(result));
                            });
                        }
                    } catch (err) {
                        this.htmlDesc = Loadable.error(err);
                    }
                }
            },

            loadTags: async function(): Promise<void> {
                console.log(`loading tags of post ${this.postID}`);
                this.tags = Loadable.loading();
                this.tags = await PostTagApi.getByPostID(this.postID);

                if (this.tags.state == "loaded") {
                    this.edit.tags = "";
                    for (const b of this.sortedTags) {
                        this.edit.tags += b.tags.sort((a, b) => a.name.localeCompare(b.name)).map(iter => iter.name).join(" ");
                        this.edit.tags += "\n";
                    }
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
            AppMenu,
            FileView,
            PostSearch
        }

    });
    export default ViewPost;

</script>