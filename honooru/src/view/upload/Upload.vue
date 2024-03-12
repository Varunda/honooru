<template>
    <div>
        <app-menu class="flex-grow-1">
            <menu-dropdown></menu-dropdown>

            <menu-sep></menu-sep>

            <li class="nav-item h1 p-0">
                Upload
            </li>
        </app-menu>

        <div v-if="state == 'upload'">
            <div class="custom-file">
                <input id="file-upload" type="file" class="custom-file-input" />
                <label class="custom-file-label" for="file-upload">Choose file</label>
            </div>

            <div class="w-100">
                <button class="btn btn-primary" @click="uploadFile">Upload</button>
            </div>

            <div v-if="upload.show == true">
                <hr class="border" />

                Uploading...
                <div class="progress mt-3" style="height: 3rem;">
                    <div class="progress-bar bg-primary" :style="{ width: uploadWidth }" style="height: 3rem;">
                        <span style="position: absolute; left: 50%; transform: translateX(-50%); font-size: 2.5rem;">
                            {{upload.progress}}/{{upload.total}}
                            ({{upload.progress / Math.max(1, upload.total) * 100 | locale(2)}}%)
                        </span>
                    </div>
                </div>
            </div>
        </div>

        <div v-else-if="state == 'processing'">
            <h2 class="pl-2">processing file</h2>

            <div v-for="e in progress.progress" class="mb-2 border-bottom">
                <h3>
                    {{e.name}}
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
            <file-view :md5="mediaAsset.md5" :file-extension="mediaAsset.fileExtension"></file-view>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import * as sR from "signalR";

    import { Loading, Loadable } from "Loading";

    import { AppMenu, MenuSep, MenuDropdown, MenuImage } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import ProgressBar from "components/ProgressBar.vue";

    import FileView from "components/app/FileView.vue";

    import { MediaAsset, MediaAssetApi } from "api/MediaAssetApi";

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

                mediaAsset: null as MediaAsset | null,

                connection: null as sR.HubConnection | null,

                progress: new UploadStepEntry() as UploadStepEntry,

                upload: {
                    show: false as boolean,
                    progress: 0 as number,
                    total: 0 as number,
                }
            }
        },

        created: function(): void {
            this.connection = new sR.HubConnectionBuilder()
                .withUrl("/ws/upload-progress")
                .withAutomaticReconnect([5000, 10000, 20000, 20000])
                .build();

            this.connection.on("UpdateProgress", this.onUpdateProgress);
            this.connection.on("Finish", this.onFinish);

            this.connection.start().then(() => {
                console.log(`connection opened`);
            });
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.makeFile();
            });
        },

        methods: {
            makeFile: function(): void {
                this.file = document.getElementById("file-upload") as HTMLInputElement | null;
            },

            uploadFile: async function(): Promise<void> {
                this.state = "upload";
                const files: FileList = (this.file as any).files;

                if (files.length == 0) {
                    console.warn(`cannot upload, 0 files selected`);
                    return;
                }

                for (let i = 0; i < files.length; ++i) {
                    const f: File | null = files.item(0);

                    if (f == null) {
                        console.warn(`failed to get file at index ${i}`);
                        continue;
                    }

                    this.upload.show = true;
                    const asset: Loading<MediaAsset> = await MediaAssetApi.upload(f, (progress: number, total: number) => {
                        this.upload.progress = progress;
                        this.upload.total = total;
                    });
                    if (asset.state == "loaded") {
                        this.upload.show = false;
                        this.upload.progress = this.upload.total = 0;
                        console.log(`uploaded ${asset.data.guid} with hash of ${asset.data.md5}`);

                        if (this.connection != null) {
                            await this.connection.invoke("SubscribeToMediaAsset", asset.data.guid);
                            console.log(`connected to hub`);
                            this.state = "processing";
                        }
                    }
                }
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
                this.state = "view";
                console.log(`done ${elem}`);
                console.log(elem);

                setTimeout(() => {
                    this.mediaAsset = elem;
                }, 500);
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
            AppMenu, MenuSep, MenuDropdown, MenuImage,
            ProgressBar,
            FileView
        }

    });
    export default Upload;
</script>