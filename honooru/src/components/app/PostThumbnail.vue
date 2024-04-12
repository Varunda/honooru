<template>
    <div style="max-width: 180px;" :class="containerClasses">
        <span v-if="post.durationSeconds > 0" class="position-absolute font-monospace bg-dark ms-2" style="font-size: 0.8rem;">
            {{post.durationSeconds | mduration}}
        </span>

        <a :href="href">
            <img :src="'/media/180x180/' + post.md5 + '.png'" style="width: 180px; height: 180px;"
                 :class="imageClasses" />
        </a>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { Post } from "api/PostApi";

    import AccountUtil from "util/AccountUtil";

    export const PostThumbnail = Vue.extend({
        props: {
            post: { type: Object as PropType<Post>, required: true },
            query: { type: String, required: false }
        },

        methods: {
        },

        computed: {

            href: function(): string {
                return `/post/${this.post.id}` + ((this.query) ? `?q=${this.query}` : "");
            },

            blurUnsafe: function(): boolean {
                return AccountUtil.getSetting("postings.unsafe.behavior")?.value == "blur";
            },

            blurExplicit: function(): boolean {
                return AccountUtil.getSetting("postings.explicit.behavior")?.value == "blur";
            },

            hideUnsafe: function(): boolean {
                return AccountUtil.getSetting("postings.unsafe.behavior")?.value == "hidden";
            },

            hideExplicit: function(): boolean {
                return AccountUtil.getSetting("postings.explicit.behavior")?.value == "hidden";
            },

            containerClasses: function(): string {
                if (this.post.rating == 2 && this.blurUnsafe) {
                    return "border border-warning rounded";
                }

                if (this.post.rating == 3 && this.blurExplicit) {
                    return "border border-danger rounded";
                }

                return "";
            },

            imageClasses: function(): string {
                if ((this.post.rating == 2 && this.blurUnsafe)
                    || (this.post.rating == 3 && this.blurExplicit)) {

                    return "blurred-image";
                }

                return "";
            }


        }

    });

    export default PostThumbnail;

</script>
