<template>
    <div>
        <app-menu></app-menu>

        <div v-if="state == 'upload'">

            <div class="d-flex">
                <div class="flex-grow-1"></div>

                <div class="flex-grow-2" style="max-width: 640px;">
                    <div class="h1 text-center">
                        upload
                    </div>

                    <div class="custom-file mb-1">
                        <input id="file-upload" type="file" class="custom-file-input" @change="updateName" />
                        <label class="custom-file-label" for="file-upload" style="color: black!important;">{{fileText}}</label>
                    </div>

                    <div>
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
                            <span class="h3 flex-grow-0 mr-2">processing</span>
                            <span class="h5 flex-grow-0">(these uploads are currently being processed, or are queued for processing)</span>
                            <span class="border px-2 mx-1 flex-grow-1" style="height: 1px;"></span>
                        </div>

                        <div v-if="upload.processing.state == 'loaded'">
                            <div v-for="entry in upload.processing.data">
                                <a :href="'/upload?m=' + entry.guid">
                                    {{entry.guid}} - {{entry.fileName}} ({{entry.timestamp | moment}})
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
                            <span class="h3 flex-grow-0 mr-2">ready</span>
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
                                    -
                                    {{entry.guid}} - {{entry.fileName}} ({{entry.timestamp | moment}})
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
            <h2 class="pl-2">processing file</h2>

            <div v-for="e in progress.progress" class="pb-2 mb-2 border-bottom">
                <h3>
                    step {{e.order + 1}} - {{e.name}}
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
        </div>

        <div v-else-if="state == 'view'">

            <div v-if="mediaAsset.postID != null" class="d-flex">
                <span class="flex-grow-1"></span>

                <span class="h1 text-center flex-grow-0 mt-5">
                    this file was already uploaded in

                    <a :href="'/post/' + mediaAsset.postID">
                        post #{{mediaAsset.postID}}
                    </a>
                </span>

                <span class="flex-grow-1"></span>
            </div>

            <div v-else style="display: grid; grid-template-columns: 400px 1fr; gap: 0.5rem;">
                <div>
                    <div class="mb-2">
                        <label class="mb-0">rating</label>
                        <div class="btn-group w-100">
                            <button class="btn" :class="[ posting.rating == 'explicit' ? 'btn-primary' : 'btn-secondary' ]" @click="posting.rating = 'explicit'">
                                explicit
                            </button>
                            <button class="btn" :class="[ posting.rating == 'unsafe' ? 'btn-primary' : 'btn-secondary' ]" @click="posting.rating = 'unsafe'">
                                unsafe
                            </button>
                            <button class="btn" :class="[ posting.rating == 'general' ? 'btn-primary' : 'btn-secondary' ]" @click="posting.rating = 'general'">
                                general
                            </button>
                        </div>
                    </div>

                    <div class="mb-2">
                        <label class="mb-0">tags</label>
                        <post-search v-model="posting.tags" type="textarea"></post-search>
                    </div>

                    <div class="mb-0">
                        <label class="mb-0">title</label>
                        <input type="text" class="form-control" v-model="posting.title" />
                    </div>

                    <div class="mb-2">
                        <label class="mb-0">description</label>
                        <textarea v-model="posting.description" class="form-control"></textarea>
                    </div>

                    <div class="mb-2">
                        <label class="mb-0">source</label>
                        <input type="text" class="form-control" v-model="posting.source" />
                    </div>

                    <div class="mb-2">
                        <label class="mb-0">
                            additional tags
                            <info-hover text="these tags will be added when posted"></info-hover>
                        </label>

                        <textarea class="form-control" v-model="posting.additionalTags" readonly disabled style="background-color: var(--gray); color: var(--dark);">
                        </textarea>
                    </div>

                    <span class="text-muted d-block">
                        Control + Enter to post
                    </span>

                    <button class="btn btn-primary" @click="makePost">
                        upload
                    </button>

                    <div v-if="posting.post.state == 'error'">
                        <api-error :error="posting.post.problem"></api-error>
                    </div>
                </div>

                <file-view :md5="mediaAsset.md5" :file-extension="mediaAsset.fileExtension"></file-view>
            </div>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import * as sR from "signalR";

    import { Loading, Loadable } from "Loading";

    import { AppMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import ProgressBar from "components/ProgressBar.vue";

    import FileView from "components/app/FileView.vue";
    import PostSearch from "components/app/PostSearch.vue";

    import { MediaAsset, MediaAssetApi } from "api/MediaAssetApi";
    import { PostApi, Post } from "api/PostApi";

    import "filters/ByteFilter";
    import "filters/MomentFilter";

    class UploadStepEntry {
        public current: UploadStepProgress | null = null;
        public progress: UploadStepProgress[] = [];
    }

    class UploadStepProgress {
        public name: string = "";
        public order: number = 0;
        public percent: number = 0;
        public finished: boolean = false;
    }

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
                    additionalTags: "" as string
                }
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
                MediaAssetApi.getByID(this.mediaAssetID).then((data: Loading<MediaAsset>) => {
                    if (data.state == "loaded") {
                        if (data.data.status == 2) { // 2 = processing
                            this.state = "processing";
                        } else if (data.data.status == 3) { // 3 = done
                            this.mediaAsset = data.data;
                            this.state = "view";

                            this.setMediaAsset(data.data);
                        } else {
                            console.log(`unchecked status: ${data.data.status}`);
                        }
                    }
                });
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
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
            getMediaAssetID: function(): string | null {
                const search: URLSearchParams = new URLSearchParams(location.search);
                const id: string | null = search.get("m");

                return id;
            },

            setMediaAssetID: function(id: string): void {
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
                    const asset: Loading<MediaAsset> = await MediaAssetApi.upload(f, (progress: number, total: number) => {
                        this.upload.progress = progress;
                        this.upload.total = total;
                        console.log(`${progress} of ${total} uploaded`);
                    });
                    await this._handleAsset(asset);
                }
            },

            _handleAsset: async function(asset: Loading<MediaAsset>): Promise<void> {
                if (asset.state == "loaded") {
                    this.mediaAsset = asset.data;

                    this.setMediaAssetID(asset.data.guid);
                    this.upload.show = false;
                    this.upload.progress = this.upload.total = 0;
                    console.log(`uploaded ${asset.data.guid} with hash of ${asset.data.md5}`);

                    if (this.connection != null) {
                        await this.connection.invoke("SubscribeToMediaAsset", asset.data.guid);
                        console.log(`connected to hub`);
                        this.state = "processing";
                    }
                }
            },

            makePost: async function(): Promise<void> {
                if (this.mediaAsset == null) {
                    console.warn(`cannot makePost: mediaAsset is null`);
                    return;
                }

                console.log(`uploading media asset ${this.mediaAsset.guid}`);

                this.posting.post = Loadable.loading();
                this.posting.post = await PostApi.upload(this.mediaAsset.guid,
                    this.posting.tags, this.posting.rating, this.posting.title, this.posting.description, this.posting.source);

                if (this.posting.post.state == "loaded") {
                    location.pathname = "/post/" + this.posting.post.data.id;
                }
            },

            updateName: function(ev: any): void {
                const files: FileList = ev.target.files;
                if (files.length > 0) {
                    this.fileText = files.item(0)?.name ?? "missing name";
                }

                console.log(ev);
            },

            onUpdateProgress: function(ev: any): void {
                try {
                    let entry: UploadStepEntry = new UploadStepEntry();
                    entry.current = ev.current;

                    for (const key in ev.progress) {
                        const s: UploadStepProgress = new UploadStepProgress();
                        s.name = key;

                        const iter: any = ev.progress[key];
                        s.order = iter.order;
                        s.percent = iter.percent;
                        s.finished = iter.finished;

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
                    "bg-secondary": e.finished == false && e.percent == 0,
                    "bg-info": e.finished == false && e.percent > 0,
                    "bg-success": e.finished == true,
                    "progress-bar-striped": e.finished == true,
                    "progress-bar-animated": e.finished == true
                }
            }
        },

        computed: {
            uploadWidth: function(): string {
                return `${this.upload.progress / Math.max(this.upload.total) * 100}%`;
            }
        },

        components: {
            InfoHover, ApiError,
            AppMenu,
            ProgressBar,
            FileView, PostSearch
        }

    });
    export default Upload;
</script>