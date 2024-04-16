<template>
    <div>
        <div v-if="FileType == 'video'">
            <video id="video-playback" controls preload="auto" class="video-js" style="max-width: 1920px;">
                <source :src="'/media/original/' + md5 + '.' + FileExtension" type="video/mp4" />
            </video>
        </div>

        <div v-else-if="FileType == 'image'">
            <img :src="'/media/original/' + md5 + '.' + FileExtension" class="mw-100" />
        </div>

        <div v-else class="text-danger">
            unchecked FileType: {{FileType}}
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import videojs, { VideoJsPlayer } from "video.js";

    export const FileView = Vue.extend({
        props: {
            md5: { type: String, required: true },
            FileType: { type: String, required: true },
            FileExtension: { type: String, required: true }
        },

        data: function() {
            return {
                player: null as VideoJsPlayer | null
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                if (this.FileType == "video") {
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

        }
    });
    export default FileView;
</script>