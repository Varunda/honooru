<template>
    <div style="display: grid; grid-template-rows: min-content 1fr; gap: 0.5rem; max-height: 100vh; max-width: 100vw;">

        <app-menu></app-menu>

        <div style="display: grid; grid-template-columns: 400px 1fr 200px; gap: 0.5rem; overflow: hidden">
            <div class="overflow-y-auto">
                <div class="mb-3">
                    <post-search v-model="query" @keyup.enter="performSearch" @do-search="performSearch"></post-search>
                </div>

                <div class="mb-3 border">
                    <div class="text-center rounded-top bg-secondary text-light">
                        rating
                    </div>
                    <div class="btn-group w-100">
                        <button class="btn rounded-0" :disabled="!editing"
                                :class="[ edit.rating == 'explicit' ? 'btn-danger' : 'btn-secondary' ]" @click="edit.rating = 'explicit'">
                            explict
                        </button>
                        <button class="btn rounded-0" :disabled="!editing" 
                                :class="[ edit.rating == 'unsafe' ? 'btn-warning' : 'btn-secondary' ]" @click="edit.rating = 'unsafe'">
                            unsafe
                        </button>
                        <button class="btn rounded-0" :disabled="!editing"
                                :class="[ edit.rating == 'general' ? 'btn-primary' : 'btn-secondary' ]" @click="edit.rating = 'general'">
                            general
                        </button>
                    </div>
                </div>

                <div class="honooru-tags" style="line-height: 1.3;">
                    <div v-if="tags.state == 'idle'"></div>
                    <div v-else-if="tags.state == 'loading'">
                        loading tags...
                    </div>

                    <div v-else-if="tags.state == 'loaded'">
                        <div v-for="block in sortedTags" class="mb-3 no-underline-links font-monospace" style="color: #212529">
                            <h6 class="mb-1 px-2 py-1 rounded" style="font-size: 1rem;" :style="{ 'background-color': '#' + block.hexColor }"
                                data-bs-toggle="collapse" :data-bs-target="'#tag-block-type-' + block.typeID">
                                <strong>{{block.name}}</strong>
                            </h6>

                            <div :id="'tag-block-type-' + block.typeID" class="collapse show">
                                <div v-for="tag in block.tags" :style="{ 'color': '#' + tag.hexColor }">
                                    <a :href="'/tag/' + tag.id" :class="{ 'text-success': tag.description, 'text-secondary': !tag.description }">
                                        <info-hover :text="tag.description || ''"></info-hover>
                                    </a>
                                    <a :href="'/posts?q=' + tag.name">
                                        <span :style="{ 'color': '#' + tag.hexColor }">
                                            {{tag.name.split("_").join(" ")}}
                                        </span>
                                    </a>
                                    <span class="" style="color: var(--bs-gray-600)">
                                        {{tag.uses}}
                                    </span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div v-if="editing == true" class="mb-1 border-top pt-2 mb-2">
                    <h5 class="mb-0">edit tags</h5>
                    <post-search ref="post-tags" v-model="edit.tags" type="textarea" @keyup.control.enter="saveEdit"></post-search>
                </div>

                <div class="mb-2">
                    <h5 class="wt-header">
                        source
                    </h5>

                    <div v-if="editing == false">
                        <code v-if="edit.source != '' && edit.source.startsWith('file://')" class="text-break">
                            {{edit.source}}
                        </code>
                        <a v-else-if="edit.source != ''" :href="edit.source" class="text-break">{{edit.source}}</a>
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

                <div v-if="editing == true" class="mb-2">
                    <label class="mb-0">
                        context
                        <info-hover text="additional information about the post that is not self-evident from the post itself"></info-hover>
                    </label>
                    <textarea v-model="edit.context" class="form-control"></textarea>
                </div>

                <div class="mb-3">
                    <permissioned-button v-if="editing == false" permission="App.Post.Edit" class="btn btn-primary" @click="startEdit">
                        edit
                    </permissioned-button>

                    <button v-if="editing == true" class="btn btn-secondary" @click="cancelEdit">
                        cancel
                    </button>

                    <button v-if="editing == true" class="btn btn-success" @click="saveEdit">
                        save
                    </button>
                </div>

                <h5 class="wt-header">
                    information
                </h5>
                <div>
                    <div v-if="post.state == 'idle'"></div>

                    <div v-else-if="post.state == 'loading'">
                        loading...
                    </div>

                    <post-info v-else-if="post.state == 'loaded'" :post="post.data" class="mb-2"></post-info>

                    <api-error v-else-if="post.state == 'error'" :error="post.problem"></api-error>
                </div>

                <h5 class="wt-header">
                    misc controls
                </h5>
                <div>
                    <button class="btn btn-primary d-block mb-1" id="pool-trigger" data-bs-toggle="offcanvas" data-bs-target="#pool-offcanvas" title="hotkey is P">
                        pools
                    </button>

                    <button class="btn btn-secondary d-block mb-1" @click="remakeThumbnail">
                        remake thumbnail
                    </button>

                    <button class="btn btn-secondary d-block mb-1" @click="updateFileType">
                        update file type
                    </button>

                    <div v-if="post.state == 'loaded'">
                        <permissioned-button v-if="post.data.status == 1" permission="App.Post.Delete" class="btn btn-danger d-block mb-1" @click="deletePost">
                            delete
                        </permissioned-button>

                        <permissioned-button v-else-if="post.data.status == 2" permission="App.Post.Restore" class="btn btn-success d-block mb-1" @click="restorePost">
                            restore
                        </permissioned-button>
                    </div>

                    <permissioned-button permission="App.Post.Erase" class="btn btn-danger d-block mb-1" @click="erasePost">
                        erase
                    </permissioned-button>
                </div>
            </div>

            <div class="overflow-y-auto" id="post-container">
                <div v-if="post.state == 'idle'"></div>

                <div v-else-if="post.state == 'loading'">
                    loading...
                </div>

                <div v-else-if="post.state == 'nocontent'">
                    failed to find post {{postID}}
                </div>

                <div v-else-if="post.state == 'loaded'">
                    <div v-if="post.data.height > 720" class="alert alert-primary">
                        view:
                        <span @click="sizing = 'fit'" :class="[ sizing == 'fit' ? 'fw-bold': 'text-muted' ]" class="text-decoration-underline">fit</span>
                        /
                        <span @click="sizing = 'full'" :class="[ sizing == 'full' ? 'fw-bold':'text-muted' ]" class="text-decoration-underline">fullsized</span>
                        /
                        <span @click="sizing = 'full_width'" :class="[ sizing == 'full_width' ? 'fw-bold':'text-muted' ]" class="text-decoration-underline">full width</span>

                        <span v-if="sizing == 'fit'">
                            (resized to {{(720 / (post.data.width > post.data.height ? post.data.width : post.data.height) * 100) | locale(0)}}%)
                        </span>
                        <span v-if="sizing == 'full'">
                            (not resized)
                        </span>
                        <span v-if="sizing == 'full_width'">
                            (resized to {{(containerWidth / (post.data.width > post.data.height ? post.data.width : post.data.height) * 100) | locale(0)}}%)
                        </span>
                    </div>

                    <file-view :md5="post.data.md5" :file-type="post.data.fileType" :file-extension="post.data.fileExtension"
                               :sizing="sizing" :width="post.data.width" :height="post.data.height">
                    </file-view>

                    <div class="mx-3">
                        <div class="h2 mt-2 pt-2">
                            <span v-if="post.data.title">
                                {{post.data.title}}
                            </span>

                            <span v-else class="text-muted">
                                no title set
                            </span>
                        </div>

                        <alert-collapse v-if="post.data.description">
                            <template v-slot:header>
                                description
                            </template>

                            <template v-slot:body>
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
                            </template>
                        </alert-collapse>

                        <alert-collapse v-if="post.data.context">
                            <template v-slot:header>
                                context
                            </template>

                            <template v-slot:body>
                                <div v-if="htmlContext.state == 'idle'"></div>

                                <div v-else-if="htmlContext.state == 'loading'">
                                    loading...
                                </div>

                                <div v-else-if="htmlContext.state == 'loaded'" v-html="htmlContext.data"></div>

                                <div v-else-if="htmlContext.state == 'error'">
                                    error loading description:
                                    <api-error :error="htmlContext.problem"></api-error>
                                </div>

                                <div v-else>
                                    unchecked state of htmlContext: {{htmlContext.state}}
                                </div>
                            </template>
                        </alert-collapse>

                        <alert-collapse>
                            <template v-slot:header>
                                post relations
                            </template>

                            <template v-slot:body>
                                <post-child-view :post-id="postID"></post-child-view>
                            </template>
                        </alert-collapse>

                        <div class="alert alert-secondary d-flex">
                            <div v-if="ordering.state == 'loaded'" class="flex-grow-1">
                                <a v-if="ordering.data.previous != null" id="post-ordering-previous" :href="'/post/' + ordering.data.previous.id + '?q=' + query" title="hotkey is A">
                                    previous
                                </a>
                                <span v-else class="text-muted">
                                    no previous post!
                                </span>
                            </div>

                            <div class="flex-grow-0">
                                search: 
                                <a :href="'/posts?q=' + query">
                                    {{query.split("_").join(" ")}}
                                </a>
                            </div>

                            <div v-if="ordering.state == 'loaded'" class="flex-grow-1 text-end">
                                <a v-if="ordering.data.next != null" id="post-ordering-next" :href="'/post/' + ordering.data.next.id + '?q=' + query" title="hotkey is D">
                                    next
                                </a>
                                <span v-else class="text-muted">
                                    no next post!
                                </span>
                            </div>
                        </div>
                    </div>

                </div>

                <api-error v-else-if="post.state == 'error'" :error="post.problem"></api-error>

                <div v-else>
                    unchecked state of post: {{post.state}}
                </div>
            </div>

            <div class="pl-2 ml-2 border-left overflow-y-auto">
                <div v-if="post.state == 'loaded' && post.data.iqdbHash == ''">
                    <span class="text-danger">
                        this post is missing an IQDB hash!
                    </span>

                    <button class="btn btn-primary" @click="regenerateIqdbHash">
                        regenerate IQDB
                    </button>
                </div>

                <similarity v-if="post.state == 'loaded'" :hash="post.data.iqdbHash" :exclude-post-id="post.data.id"></similarity>
            </div>

        </div>

        <div id="pool-offcanvas" class="offcanvas offcanvas-end" tabindex="-1">
            <div class="offcanvas-header">
                <h5>
                    pools
                </h5>
                <button type="button" class="btn-close" data-bs-dismiss="offcanvas"></button>
            </div>

            <div class="offcanvas-body">
                <post-pool-view v-if="postID != 0" :post-id="postID"></post-pool-view>
            </div>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";
    import Toaster from "Toaster";
    import AccountUtil from "util/AccountUtil";
    import MarkdownUtil from "util/Markdown";

    import { AppMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import PermissionedButton from "components/PermissionedButton.vue";
    import AlertCollapse from "components/AlertCollapse.vue";

    import FileView from "components/app/FileView.vue";
    import PostSearch from "components/app/PostSearch.vue";
    import Similarity from "components/app/Similarity.vue";
    import PostChildView from "components/app/PostChildView.vue";
    import PostPoolView from "components/app/PostPoolView.vue";

    import PostInfo from "./components/PostInfo.vue";

    import { Post, PostApi, PostOrdering } from "api/PostApi";
    import { ExtendedTag, TagApi } from "api/TagApi";
    import { PostTagApi } from "api/PostTagApi";
    import { PostPool, PostPoolApi } from "api/PostPoolApi";

    import "filters/ByteFilter";
    import "filters/MomentFilter";
    import "filters/LocaleFilter";

    class TagBlock {
        public typeID: number = 0;
        public name: string = "";
        public hexColor: string = "";
        public order: number = 0;
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
                ordering: Loadable.idle() as Loading<PostOrdering>,

                tags: Loadable.idle() as Loading<ExtendedTag[]>,

                edit: {
                    tags: "" as string,
                    title: "" as string,
                    description: "" as string,
                    context: "" as string,
                    source: "" as string,
                    rating: "" as string
                },

                pools: {
                    data: Loadable.idle() as Loading<PostPool[]>,
                    create: "" as string
                },

                query: "" as string,

                editing: false as boolean,

                sizing: "fit" as string,

                htmlDesc: Loadable.idle() as Loading<string>,
                htmlContext: Loadable.idle() as Loading<string>,

                containerElement: null as HTMLElement | null,
                containerWidth: 0 as number
            }
        },

        created: function(): void {
            document.title = "Honooru / Post";

            const params: URLSearchParams = new URLSearchParams(location.search);
            if (params.has("q")) {
                this.query = params.get("q")!;
            } else {
                this.query = "";
            }
        },

        mounted: function(): void {
            this.getPostID();
            this.loadAll();

            this.sizing = AccountUtil.getSetting("posting.sizing")?.value ?? "fit";

            // add observer to the post-container resizing for showing the post resize controls
            this.containerElement = document.getElementById("post-container");
            if (this.containerElement != null) {
                const obs = new ResizeObserver((mutations: ResizeObserverEntry[], observer: ResizeObserver) => {
                    for (const mut of mutations) {
                        this.containerWidth = mut.contentRect.width;
                        console.log(`ViewPost> post container width changed to: ${this.containerWidth}`);
                    }
                });

                obs.observe(this.containerElement);
            }

            document.addEventListener("keyup", (ev: KeyboardEvent) => {
                // this means another input is currently in focus
                if (document.activeElement != document.body) {
                    return;
                }

                if (ev.key == "e") {
                    this.startEdit();
                }

                if (ev.key == "a") {
                    document.getElementById("post-ordering-previous")?.click();
                } 

                if (ev.key == "d") {
                    document.getElementById("post-ordering-next")?.click();
                }

                if (ev.key == "p") {
                    document.getElementById("pool-trigger")?.click();
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

                if (Number.isNaN(this.postID)) {
                    console.error(`invalid post ID, turns into NaN! ${parts[2]}`);
                }

                document.title = `Honooru / Post #${this.postID}`;
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
                    this.edit.context = this.post.data.context;

                    this.htmlDesc = Loadable.loading();
                    MarkdownUtil.markdown(this.post.data.description ?? "").then((markdown: string) => {
                        this.htmlDesc = Loadable.loaded(markdown);
                    }).catch((err: any) => {
                        this.htmlDesc = Loadable.error(err);
                    });

                    this.htmlContext = Loadable.loading();
                    MarkdownUtil.markdown(this.post.data.context).then((markdown: string) => {
                        this.htmlContext = Loadable.loaded(markdown);
                    }).catch((err: any) => {
                        this.htmlContext = Loadable.error(err);
                    });
                }

                this.ordering = Loadable.loading();
                PostApi.getOrdering(this.query, this.postID).then((l: Loading<PostOrdering>) => {
                    this.ordering = l;
                });
            },

            loadTags: async function(): Promise<void> {
                console.log(`loading tags of post ${this.postID}`);
                this.tags = Loadable.loading();
                this.tags = await PostTagApi.getByPostID(this.postID);

                // put each tag category on a different line
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
                await PostApi.update(this.postID, this.edit.tags, this.edit.rating, this.edit.source, this.edit.title, this.edit.description, this.edit.context);

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
                Toaster.add("success", `submitted post ${this.post.data.id} for a new thumbnail`, "success");
            },

            updateFileType: async function(): Promise<void> {
                if (this.post.state != "loaded") {
                    return console.warn(`cannot update file type: post is not loaded`);
                }

                const l: Loading<void> = await PostApi.updateFileType(this.post.data.id);
                if (l.state == "loaded") {
                    Toaster.add("success", `updated the file type of post ${this.post.data.id}`, "success");
                } else if (l.state == "error") {
                    Loadable.toastError(l, "failed to update file type");
                } else {
                    console.error(`unchecked response state (updateFileType): ${l.state}`);
                }
            },

            performSearch: function(query: string): void {
                const url = new URL(location.href);
                url.searchParams.set("q", query.trim());
                history.pushState({ path: url.href }, "", `/posts?${url.searchParams.toString()}`);
                location.href = `/posts?${url.searchParams.toString()}`;
            },

            regenerateIqdbHash: async function(): Promise<void> {
                const l: Loading<void> = await PostApi.regenerateIqdb(this.postID);
                if (l.state == "loaded") {
                    Toaster.add(`IQDB updated`, `successfully regenerated IQDB hash for ${this.postID}`, "success");
                    await this.loadAll();
                } else {
                    console.error(`unchecked state from regenerate iqdb hash: ${l.state}`);
                }
            },

            deletePost: async function(): Promise<void> {
                const l: Loading<void> = await PostApi.remove(this.postID);
                console.log(`delete post response: ${l.state}`);
                if (l.state == "loaded") {
                    Toaster.add("post deleted", `successfully deleted post ${this.postID}`, "success");
                    await this.loadAll();
                } else {
                    console.error(`unchecked state from delete: ${l.state}`);
                }
            },

            restorePost: async function(): Promise<void> {
                const l: Loading<void> = await PostApi.restore(this.postID);
                if (l.state == "loaded") {
                    Toaster.add("post restored", `successfully restored post ${this.postID}`, "success");
                    await this.loadAll();
                } else {
                    console.error(`unchecked state from restore: ${l.state}`);
                }
            },

            erasePost: async function(): Promise<void> {
                if (prompt("Are you sure you want to do this? This CANNOT BE REVERSED. Type the post ID to confirm") != this.postID.toString()) {
                    return;
                }

                const l: Loading<void> = await PostApi.erase(this.postID);
                if (l.state == "loaded") {
                    Toaster.add("post erased", `successfully erased post ${this.postID}`, "success");
                    await this.loadAll();
                } else {
                    console.error(`unchecked state from erase: ${l.state}`);
                }
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
                        block.order = tag.typeOrder;
                        block.typeID = tag.typeID;
                    }

                    block.tags.push(tag);

                    map.set(tag.typeID, block);
                }

                const blocks: TagBlock[] = Array.from(map.values()).sort((a, b) => {
                    return a.order - b.order;
                });

                for (const b of blocks) {
                    b.tags.sort((a, b) => {
                        return a.name.localeCompare(b.name);
                    });
                }

                return blocks;
            },

            permissions: function() {
                return {
                    post: {
                        delete: AccountUtil.hasPermission("app.post.delete"),
                        erase: AccountUtil.hasPermission("app.post.erase")
                    }

                };
            }
        },

        components: {
            InfoHover, ApiError, PermissionedButton, AlertCollapse,
            AppMenu,
            FileView, PostSearch, Similarity, PostChildView, PostPoolView,
            PostInfo
        }

    });
    export default ViewPost;

</script>