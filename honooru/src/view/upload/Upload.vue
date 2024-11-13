<template>
    <div style="display: grid; grid-template-rows: min-content 1fr; gap: 0.5rem; max-height: 100vh; max-width: 100vw;">
        <app-menu></app-menu>

        <div v-if="state == 'upload'">
            <div class="d-flex">
                <div class="flex-grow-1"></div>

                <div class="flex-grow-2" style="max-width: 640px;">
                    <div class="h1 text-center">
                        upload
                    </div>

                    <div class="input-group mb-3">
                        <input id="file-upload" type="file" class="form-control" @change="updateName" />
                        <label class="custom-file-label" for="file-upload"></label>
                    </div>

                    <div class="mb-3">
                        <input class="form-control" type="text" v-model="inputUrl" placeholder="URL" />
                    </div>

                    <div class="w-100">
                        <button class="btn btn-primary w-100" @click="doUpload">Upload</button>
                    </div>

                    <div v-if="upload.show == true">
                        <hr class="border" />

                        Uploading...
                        <div class="progress mt-3" style="height: 3rem;">
                            <div class="progress-bar bg-primary" :style="{ width: uploadWidth }" style="height: 3rem;">
                                <span style="position: absolute; left: 50%; transform: translateX(-50%); font-size: 2.5rem;">
                                    {{upload.progress | bytes}}/{{upload.total | bytes}}
                                    ({{upload.progress / Math.max(1, upload.total) * 100 | locale(2)}}%)
                                </span>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="flex-grow-1"></div>
            </div>

            <div>
                <div class="mb-3">
                    <div>
                        <div class="d-flex align-items-center">
                            <span class="border px-2 mx-1" style="max-width: 30px; flex-grow: 1; height: 1px;"></span>
                            <span class="h3 flex-grow-0 me-2">processing</span>
                            <span class="h5 flex-grow-0">(these uploads are currently being processed, or are queued for processing)</span>
                            <span class="border px-2 mx-1 flex-grow-1" style="height: 1px;"></span>
                        </div>

                        <div v-if="upload.processing.state == 'loaded'">
                            <div v-for="entry in upload.processing.data">
                                <a :href="'/upload?m=' + entry.guid">
                                    <code>{{entry.guid}}</code> - {{entry.fileName}} ({{entry.timestamp | moment}})
                                </a>
                            </div>
                            <div v-if="upload.processing.data.length == 0" class="text-muted">
                                nothing!
                            </div>
                        </div>
                    </div>
                </div>

                <div>
                    <div>
                        <div class="d-flex align-items-center">
                            <span class="border px-2 mx-1" style="max-width: 30px; flex-grow: 1; height: 1px;"></span>
                            <span class="h3 flex-grow-0 me-2">ready</span>
                            <span class="h5 flex-grow-0">(these uploads are ready to be tagged)</span>
                            <span class="border px-2 mx-1 flex-grow-1" style="height: 1px;"></span>
                        </div>

                        <div v-if="upload.ready.state == 'loading'">
                            <span class="text-muted">
                                loading...
                            </span>
                        </div>

                        <div v-if="upload.ready.state == 'loaded'">
                            <a v-for="entry in upload.ready.data" :href="'/upload?m=' + entry.guid" class="d-block">
                                <span>
                                    <code>{{entry.guid}}</code> - {{entry.fileName}} ({{entry.timestamp | moment}})
                                </span>
                            </a>
                            <div v-if="upload.ready.data.length == 0" class="text-muted">
                                nothing!
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div v-else-if="state == 'processing'">
            <h2 class="ps-2">
                processing file 
                <span v-if="mediaAsset != null" class="fs-5">
                    (<code>{{mediaAsset.source}}</code>)
                </span>
            </h2>

            <div v-if="progress.error != null" class="alert alert-danger mb-3">
                an error of
                <strong>{{progress.error.type}}</strong>
                occured in
                <strong>{{progress.error.source}}</strong>
                while uploading:
                <strong>{{progress.error.message}}</strong>

                <code><pre>{{progress.error.stackTrace}}</pre></code>
            </div>

            <div v-for="e in progress.progress" class="pb-3 mb-3 border-bottom">
                <h3>
                    step {{e.order + 1}} - {{e.name}}
                    <span v-if="e.finished == true" class="text-success">
                        (done)
                    </span>
                    <span v-if="e.finished == false && e.percent > 0" class="text-info">
                        (in progress, estimated {{computeCurrentStepTimeLeft | mduration}} left)
                    </span>
                </h3>

                <div class="progress" style="height: 2rem">
                    <div class="progress-bar" :class="computeProgressClasses(e)"
                         :style="{ 'width': e.percent + '%' }" style="height: 2rem;">

                        <span style="font-size: 1.5rem;">
                            {{e.percent}}%
                        </span>
                    </div>
                </div>
            </div>

            <button class="btn btn-danger" @click="deleteUpload">
                delete
            </button>
        </div>

        <template v-else-if="state == 'view'">
            <div v-if="mediaAsset.postID != null">
                <div class="d-flex">
                    <span class="flex-grow-1"></span>

                    <span class="h1 text-center flex-grow-0 mt-5">
                        this file was already uploaded in

                        <a :href="'/post/' + mediaAsset.postID">
                            post #{{mediaAsset.postID}}
                        </a>
                    </span>

                    <span class="flex-grow-1"></span>
                </div>

                <button class="btn btn-danger" @click="deleteUpload">
                    delete
                </button>
            </div>

            <div v-else style="display: grid; grid-template-columns: 400px 1fr 200px; gap: 0.5rem; overflow: hidden">
                <div class="overflow-y-auto">
                    <div class="mb-2">
                        <label class="mb-0">rating</label>
                        <div class="btn-group w-100">
                            <button class="btn" :class="[ posting.rating == 'explicit' ? 'btn-danger' : 'btn-secondary' ]" @click="posting.rating = 'explicit'">
                                explicit
                            </button>
                            <button class="btn" :class="[ posting.rating == 'unsafe' ? 'btn-warning' : 'btn-secondary' ]" @click="posting.rating = 'unsafe'">
                                unsafe
                            </button>
                            <button class="btn" :class="[ posting.rating == 'general' ? 'btn-primary' : 'btn-secondary' ]" @click="posting.rating = 'general'">
                                general
                            </button>
                        </div>

                        <div v-if="error.rating == true" class="is-invalid text-danger">
                            missing rating!
                        </div>
                    </div>

                    <div class="mb-2">
                        <label class="mb-0">tags</label>
                        <post-search v-model="posting.tags" type="textarea" @keyup.control.enter="makePost"></post-search>

                        <div v-if="error.tags == true" class="text-danger">
                            missing tags!
                        </div>
                    </div>

                    <div class="mb-3">
                        <label class="mb-0">title</label>
                        <input type="text" class="form-control" v-model="posting.title" />
                    </div>

                    <div class="mb-3">
                        <label class="mb-0">description</label>
                        <textarea v-model="posting.description" class="form-control"></textarea>
                    </div>

                    <div class="mb-3">
                        <label class="mb-0">context</label>
                        <textarea v-model="posting.context" class="form-control"></textarea>
                    </div>

                    <div class="mb-3">
                        <label class="mb-0">source</label>
                        <input type="text" class="form-control" v-model="posting.source" />

                        <div v-if="isYoutubeLink && posting.source == ''">
                            suggested:
                            <br />

                            <div v-for="source in sourceMatches" class="input-group mb-1">
                                <input disabled readonly :value="source" class="form-control" />
                                <div class="input-group-append">
                                    <button class="btn btn-primary rounded-0" @click="useSource(source)">
                                        use
                                    </button><a class="btn btn-success rounded-0 rounded-end" target="_blank" :href="source">
                                        view
                                    </a>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="mb-3">
                        <label class="mb-0">
                            additional tags
                            <info-hover text="these tags will be added when posted"></info-hover>
                        </label>

                        <textarea class="form-control" v-model="posting.additionalTags" readonly disabled
                                  style="background-color: var(--bs-gray-dark); color: var(--bs-gray-light); cursor: not-allowed">
                        </textarea>
                    </div>

                    <span class="text-muted d-block">
                        Control + Enter to post
                    </span>

                    <button class="btn btn-primary" @click="makePost" :disabled="!canPost">
                        upload
                    </button>

                    <div v-if="posting.post.state == 'error'">
                        <api-error :error="posting.post.problem"></api-error>
                    </div>

                    <hr class="border" />

                    <div>
                        <strong>filename:</strong>
                        <code>{{mediaAsset.fileName}}</code>
                    </div>

                    <hr class="border" />

                    <div>
                        <button class="btn btn-danger" @click="deleteUpload">
                            delete
                        </button>
                    </div>

                </div>

                <div class="overflow-y-auto">
                    <file-view :md5="mediaAsset.md5" :file-type="mediaAsset.fileType" :file-extension="mediaAsset.fileExtension"
                               sizing="full" :width="null" :height="null">
                    </file-view>
                </div>

                <div class="overflow-y-auto pl-2 ml-2 border-left">
                    <button class="w-100 btn btn-primary" @click="recomputeIqdbHash">
                        regenerate IQDB hash
                    </button>

                    <div v-if="mediaAsset.iqdbHash == null" class="text-danger">
                        hash is not set!
                    </div>

                    <similarity v-else :hash="mediaAsset.iqdbHash" :exclude-media-asset-id="mediaAsset.guid"></similarity>
                </div>
            </div>
        </template>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import * as sR from "signalR";
    import Toaster from "Toaster";
    import { Loading, Loadable } from "Loading";

    import { AppMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import ProgressBar from "components/ProgressBar.vue";

    import FileView from "components/app/FileView.vue";
    import PostSearch from "components/app/PostSearch.vue";
    import Similarity from "components/app/Similarity.vue";

    import { MediaAsset, MediaAssetApi } from "api/MediaAssetApi";
    import { PostApi, Post, IqdbSearchResult } from "api/PostApi";

    import "filters/ByteFilter";
    import "filters/MomentFilter";
    import "filters/LocaleFilter";

    class ExceptionInfo {


    }

    class UploadStepEntry {
        public current: UploadStepProgress | null = null;
        public progress: UploadStepProgress[] = [];
        public error: ExceptionInfo | null = null;
    }

    class UploadStepProgress {
        public name: string = "";
        public order: number = 0;
        public percent: number = 0;
        public finished: boolean = false;
        public startedAt: Date = new Date();

        public static read(elem: any): UploadStepProgress {
            return {
                ...elem,
                startedAt: new Date(elem.startedAt)
            }
        }
    }

    //
    // TODO:
    //      this code fucking sucks
    //

    export const Upload = Vue.extend({
        props: {

        },

        data: function() {
            return {
                file: null as HTMLInputElement | null,
                state: "upload" as "upload" | "processing" | "view",
                fileText: "Choose file" as string,

                mediaAssetID: null as string | null,
                mediaAsset: null as MediaAsset | null,

                connection: null as sR.HubConnection | null,

                progress: new UploadStepEntry() as UploadStepEntry,

                inputUrl: "" as string,

                upload: {
                    show: false as boolean,
                    progress: 0 as number,
                    total: 0 as number,

                    processing: Loadable.idle() as Loading<MediaAsset[]>,
                    ready: Loadable.idle() as Loading<MediaAsset[]>
                },

                posting: {
                    post: Loadable.idle() as Loading<Post>,
                    tags: "" as string,
                    rating: "" as string,
                    source: "" as string,
                    title: "" as string,
                    description: "" as string,
                    context: "" as string,
                    additionalTags: "" as string
                },
            }
        },

        created: function(): void {
            this.mediaAssetID = this.getMediaAssetID();

            this.connection = new sR.HubConnectionBuilder()
                .withUrl("/ws/upload-progress")
                .withAutomaticReconnect([5000, 10000, 20000, 20000])
                .build();

            this.connection.on("UpdateProgress", this.onUpdateProgress);
            this.connection.on("Finish", this.onFinish);

            this.connection.start().then(() => {
                console.log(`connection opened`);

                if (this.mediaAssetID != null) {
                    // not sure how this could be null now
                    this.connection?.invoke("SubscribeToMediaAsset", this.mediaAssetID);
                }
            });

            if (this.mediaAssetID != null) {
                console.log(`checking if the media asset ${this.mediaAssetID} is done processing or not`);
                this.bindMediaAsset(this.mediaAssetID);
            }
        },

        mounted: function(): void {
            document.title = "Honooru / Upload";

            this.$nextTick(() => {
                document.addEventListener("keyup", (ev: KeyboardEvent) => {
                    // this means another input is currently in focus
                    if (document.activeElement != document.body) {
                        return;
                    }

                    if (ev.key == "Enter" && ev.ctrlKey) {
                        this.makePost();
                    }
                });

                if (this.state == "upload") {
                    MediaAssetApi.getProcessing().then((data: Loading<MediaAsset[]>) => {
                        this.upload.processing = data;
                    });

                    MediaAssetApi.getReady().then((data: Loading<MediaAsset[]>) => {
                        this.upload.ready = data;
                    });

                    this.makeFile();
                }
            });
        },

        methods: {
            bindMediaAsset: async function(assetID: string): Promise<void> {
                const data: Loading<MediaAsset> = await MediaAssetApi.getByID(assetID);
                if (data.state == "loaded") {
                    this.mediaAsset = data.data;

                    if (data.data.status == 2 || data.data.status == 5) { // 2 = processing, 5 = errored
                        this.state = "processing";
                    } else if (data.data.status == 3) { // 3 = done
                        this.state = "view";

                        this.setMediaAsset(data.data);
                    } else {
                        console.error(`unchecked status: ${data.data.status}`);
                    }
                }
            },

            getMediaAssetID: function(): string | null {
                const search: URLSearchParams = new URLSearchParams(location.search);
                const id: string | null = search.get("m");

                return id;
            },

            setMediaAssetID: function(id: string): void {
                this.mediaAssetID = id;
                const url = new URL(location.href);
                url.searchParams.set("m", id);
                history.pushState({ path: url.href }, "", `/upload?${url.searchParams.toString()}`);
            },

            setMediaAsset: function(asset: MediaAsset): void {
                this.posting.source = asset.source.trim();
                this.posting.title = asset.title.trim();
                this.posting.description = asset.description.trim();
                this.posting.additionalTags = asset.additionalTags.trim();
            },

            makeFile: function(): void {
                this.file = document.getElementById("file-upload") as HTMLInputElement | null;
            },

            updateTags: function(tags: string): void {
                this.posting.tags = tags;
            },

            doUpload: async function(): Promise<void> {
                if (this.inputUrl != "") {
                    console.log(`using inputUrl as upload source`);
                    await this.uploadUrl();
                } else {
                    console.log(`using file input as upload source`);
                    await this.uploadFile();
                }
            },

            uploadUrl: async function(): Promise<void> {
                const asset: Loading<MediaAsset> = await MediaAssetApi.uploadUrl(this.inputUrl);
                await this._handleAsset(asset);
            },

            uploadFile: async function(): Promise<void> {
                this.state = "upload";
                const files: FileList = (this.file as any).files;

                if (files.length == 0) {
                    console.warn(`cannot upload, 0 files selected`);
                    return;
                }

                for (let i = 0; i < files.length; ++i) {
                    const f: File | null = files.item(i);

                    if (f == null) {
                        console.warn(`failed to get file at index ${i}`);
                        continue;
                    }

                    this.upload.show = true;

                    if (f.size > 1024 * 1024 * 80) {
                        const asset: Loading<MediaAsset> = await MediaAssetApi.uploadChunk(f, (progress: number, total: number) => {
                            this.upload.progress = progress;
                            this.upload.total = total;
                            console.log(`${progress} of ${total} uploaded`);
                        });
                        await this._handleAsset(asset);
                    } else {
                        const asset: Loading<MediaAsset> = await MediaAssetApi.upload(f, (progress: number, total: number) => {
                            this.upload.progress = progress;
                            this.upload.total = total;
                            console.log(`${progress} of ${total} uploaded`);
                        });
                        await this._handleAsset(asset);
                    }
                }
            },

            _handleAsset: async function(asset: Loading<MediaAsset>): Promise<void> {
                console.log(asset.state);
                if (asset.state == "loaded") {
                    this.mediaAsset = asset.data;
                    this.setMediaAsset(asset.data);

                    this.setMediaAssetID(asset.data.guid);
                    this.upload.show = false;
                    this.upload.progress = this.upload.total = 0;
                    console.log(`uploaded ${asset.data.guid} with hash of ${asset.data.md5}`);

                    if (asset.data.status == 3) { // 3 = DONE
                        console.log(`media asset done upload, going to tag list`);
                        this.state = "view";
                    } else if (this.connection != null) {
                        await this.connection.invoke("SubscribeToMediaAsset", asset.data.guid);
                        console.log(`connected to hub`);
                        this.state = "processing";
                    } else if (this.connection == null) {
                        console.error(`expected connection to not be null here!`);
                    }
                } else if (asset.state == "error") {
                    console.error(`failed to upload asset: ${asset.problem.detail}`);
                    Toaster.add("upload failed!", `failed to upload:<br><code>${asset.problem.title}</code>`, "danger");
                }
            },

            makePost: async function(): Promise<void> {
                if (this.mediaAsset == null) {
                    console.warn(`cannot makePost: mediaAsset is null`);
                    return;
                }

                if (this.error.rating == true || this.error.tags == true) {
                    return;
                }

                console.log(`uploading media asset ${this.mediaAsset.guid}`);

                this.posting.post = Loadable.loading();
                this.posting.post = await PostApi.upload(this.mediaAsset.guid,
                    this.posting.tags, this.posting.rating, this.posting.title,
                    this.posting.description, this.posting.source, this.posting.context);

                if (this.posting.post.state == "loaded") {
                    location.pathname = "/post/" + this.posting.post.data.id;
                }
            },

            updateName: function(ev: any): void {
                const files: FileList = ev.target.files;
                if (files.length > 0) {
                    this.fileText = files.item(0)?.name ?? "missing name";
                }
            },

            onUpdateProgress: function(ev: any): void {
                try {
                    let entry: UploadStepEntry = new UploadStepEntry();
                    entry.current = ev.current == null ? null : UploadStepProgress.read(ev.current);
                    entry.error = ev.error == null ? null : ev.error;

                    for (const key in ev.progress) {
                        const s: UploadStepProgress = new UploadStepProgress();
                        s.name = key;

                        const iter: any = ev.progress[key];
                        s.order = iter.order;
                        s.percent = iter.percent;
                        s.finished = iter.finished;
                        s.startedAt = new Date(iter.startedAt);

                        entry.progress.push(s);
                    }

                    entry.progress.sort((a, b) => {
                        return a.order - b.order;
                    });

                    this.progress = entry;
                } catch (err) {
                    console.error(err);
                }
            },

            onFinish: function(elem: any): void {
                this.mediaAsset = MediaAssetApi.parse(elem);
                this.setMediaAsset(this.mediaAsset);
                this.state = "view";
                console.log(`done ${elem}`);
                console.log(elem);
            },

            computeProgressClasses: function(e: UploadStepProgress) {
                return {
                    "bg-secondary": e.finished == false && e.percent == 0 && this.progress.error == null,
                    "bg-info": e.finished == false && e.percent > 0,
                    "bg-success": e.finished == true && this.progress.error == null,
                    "bg-danger": this.progress.error != null,
                    "progress-bar-striped": e.finished == false && e.percent > 0,
                    "progress-bar-animated": e.finished == false && e.percent > 0
                }
            },

            recomputeIqdbHash: async function(): Promise<void> {
                if (this.mediaAssetID == null) {
                    console.warn(`cannot recompute iqdb hash: mediaAssetID is null`);
                    return;
                }

                const l: Loading<void> = await MediaAssetApi.regenerateIqdb(this.mediaAssetID);
                if (l.state == "loaded") {
                    Toaster.add("done!", "regenerated IQDB hash", "success");
                    this.bindMediaAsset(this.mediaAssetID);
                } else if (l.state == "error") {
                    Loadable.toastError(l, "regenerating iqdb hash");
                }
            },

            deleteUpload: async function(): Promise<void> {
                if (this.mediaAssetID == null) {
                    console.error(`media asset is null, not deleting upload`);
                    return;
                }

                const l: Loading<void> = await MediaAssetApi.remove(this.mediaAssetID);
                if (l.state == "loaded") {
                    location.href = "/upload";
                    return;
                } else if (l.state == "error") {
                    Loadable.toastError(l, "delete media asset");
                } else {
                    console.error(`unchecked state from deleting a media asset: ${l.state}`);
                }
            },

            useSource: function(suggestion: string): void {
                this.posting.source = suggestion;
            }
        },

        computed: {
            uploadWidth: function(): string {
                return `${this.upload.progress / Math.max(this.upload.total) * 100}%`;
            },

            computeCurrentStepTimeLeft: function(): number {
                if (this.progress.current == null) {
                    return -1;
                }

                const start: number = this.progress.current.startedAt.getTime();
                const left: number = 100 - this.progress.current.percent;
                const timeDiff: number = Date.now() - start;
                console.log(`start=${start}, left=${left}, timeDiff=${timeDiff}`);
                return left / (this.progress.current.percent / Math.max(1, timeDiff)) / 1000;
            },

            error: function() {
                return {
                    // not sure why these casts are needed. auto-complete can see these properties, and frontend doesn't complain
                    rating: (this as any).posting.rating == "",
                    tags: (this as any).posting.tags == ""
                }
            },

            canPost: function(): boolean {
                return this.error.rating == false
                    && this.error.tags == false;
            },

            sourceMatches: function(): string[] {
                const matches: string[] = [];

                const yt = this.mediaAsset.fileName.match(/\[(([a-zA-Z0-9_-]){10,})\]/);
                if (yt != null && yt.length >= 2) {
                    matches.push(`https://youtube.com/watch?v=${yt[1]}`);
                }

                const twitch = this.mediaAsset.fileName.match(/\[v(([0-9]){8,})\]/);
                if (twitch != null && twitch.length >= 2) {
                    matches.push(`https://twitch.tv/videos/${twitch[1]}`);
                }

                return matches;
            },

            isYoutubeLink: function(): boolean {
                return this.youtubeLink != null;
            },

            youtubeLink: function(): string | null {
                const match = this.mediaAsset.fileName.match(/\[(([a-zA-Z0-9_-]){10,})\]/);
                if (match == null || match.length < 2) {
                    return null;
                }
                return `https://youtube.com/watch?v=${match[1]}`;
            }
        },

        components: {
            InfoHover, ApiError,
            AppMenu,
            ProgressBar,
            FileView, PostSearch, Similarity
        }
    });
    export default Upload;
</script>