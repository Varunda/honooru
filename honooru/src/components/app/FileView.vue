<template>
    <div>
        <div v-if="fileType == 'video'">
            <video id="video-playback" controls preload="auto" class="video-js" style="max-width: 1920px;">
                <source :src="'/media/original/' + md5 + '.' + FileExtension" type="video/mp4" />
            </video>
        </div>

        <div v-else-if="fileType == 'image'">
            <img :src="'/media/origina/' + md5 + FileExtension" />
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import videojs, { VideoJsPlayer } from "video.js";

    export const FileView = Vue.extend({
        props: {
            md5: { type: String, required: true },
            FileExtension: { type: String, required: true }
        },

        data: function() {
            return {
                player: null as VideoJsPlayer | null
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                if (this.fileType == "video") {
                    this.makeVideo();
                }
            });
        },

        beforeDestroy: function(): void {
            if (this.player != null) {
                console.log(`dispoing of old player`);
                this.player.dispose();
            }
        },

        methods: {
            makeVideo: function(): void {
                if (this.player != null) {
                    this.player.dispose();
                    this.player = null;
                }

                this.player = videojs("video-playback", {
                    preload: "auto",
                    fluid: true
                });
            }
        },

        computed: {

            fileType: function(): string {
                const f = this.FileExtension;
                if (f == "webm" || f == "mkv" || f == "mp4" || f == "m4v") {
                    return "video";
                }

                if (f == "png" || f == "jpg" || f == "jpeg" || f == "webp") {
                    return "image";
                }

                console.error(`unchecked file extension '${f}'', assuming an 'image' file type! this is likely wrong!`);

                return "image";
            }

        }
    });
    export default FileView;
</script>