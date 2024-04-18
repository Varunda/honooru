<template>
    <div>
        <video v-if="FileType == 'video'" id="video-playback" controls preload="auto" class="video-js" style="max-width: 1920px;">
            <source :src="'/media/original/' + md5 + '.' + FileExtension" type="video/mp4" />
        </video>

        <img v-else-if="FileType == 'image'" :width="widthSize" :height="heightSize" :class="[ classWidth, classHeight ]" :src="'/media/original/' + md5 + '.' + FileExtension" />

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
            FileExtension: { type: String, required: true },

            sizing: { type: String, required: true },
            height: { type: Number, required: true },
            width: { type: Number, required: true }
        },

        data: function() {
            return {
                player: null as VideoJsPlayer | null,

                staticClass: "" as string,
                staticStyle: "" as string
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
            classWidth: function(): string | null {
                if (this.sizing == "fit") {
                    return null;
                }
                if (this.sizing == "full_width") {
                    return "mw-100";
                }

                return null;
            },

            widthSize: function(): string | null {
                if (this.sizing == "fit") {
                    if (this.width < this.height) {
                        return null;
                    } else {
                        return "720";
                    }
                }

                if (this.sizing == "full") {
                    return `${this.width}`;
                }

                return null;
            },

            classHeight: function(): string | null {
                if (this.sizing == "fit") {
                    return null;
                }
                if (this.sizing == "full_width") {
                    return "mw-100";
                }

                return null;
            },

            heightSize: function(): string | null {
                if (this.sizing == "fit") {
                    if (this.width > this.height) {
                        return null;
                    } else {
                        return "720";
                    }
                }

                if (this.sizing == "full") {
                    return `${this.height}`;
                }

                return null;
            }

        }
    });
    export default FileView;
</script>