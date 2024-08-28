<template>
    <div :id="'post-thumbnail-' + post.id" style="max-width: 180px;" :class="containerClasses" class="d-inline-block" @mouseenter="mouseOver" @mouseleave="mouseLeave">
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
    import { Loading, Loadable } from "Loading";

    import { Post } from "api/PostApi";
    import { PostTag, PostTagApi } from "api/PostTagApi";
    import { ExtendedTag } from "api/TagApi";

    import AccountUtil from "util/AccountUtil";

    import * as bootstrap from "bootstrap";

    export const PostThumbnail = Vue.extend({
        props: {
            post: { type: Object as PropType<Post>, required: true },
            query: { type: String, required: false }
        },

        data: function() {
            return {
                tags: Loadable.idle() as Loading<ExtendedTag[]>,
                popper: null as bootstrap.Popover | null,

                showing: false as boolean,

                hoverDelay: -1 as number
            }
        },

        methods: {

            mouseOver: async function(): Promise<void> {
                this.hoverDelay = setTimeout(() => {
                    this.startHover();
                }, 300) as unknown as number;

                console.log(`mouse over ${this.post.id}`);
            },

            startHover: async function(): Promise<void> {
                this.showing = true;
                console.log(`start hover ${this.post.id}`);
                if (this.popper == null) {
                    this.popper = new bootstrap.Popover(`#post-thumbnail-${this.post.id}`, {
                        trigger: "manual",
                        title: `post #${this.post.id}`,
                        customClass: "post-thumbnail-popper",
                        html: true,
                        sanitize: false // don't strip style attr from the tag colors
                    });
                    console.log(`created popover ${this.post.id}`);
                }

                if (this.tags.state == "idle") {
                    console.log(`tags is idle, loading tags ${this.post.id}`);
                    this.tags = Loadable.loading();
                    this.tags = await PostTagApi.getByPostID(this.post.id);
                }

                if (this.tags.state == "loaded") {
                    let html: string = `<div class="fs-5 border-bottom mb-1 pb-1">post #${this.post.id}</div>`;
                    for (let i = 0; i < this.tags.data.length; ++i) {
                        const tag = this.tags.data[i];
                        // the <wbr> tells the html when to break
                        const tagHtml: string = `<span style="color: #${tag.hexColor}">${tag.name}&nbsp;</span><wbr>`;

                        html += tagHtml;

                        if (html.length > 750) {
                            html += `<div>plus ${this.tags.data.length - i - 1} more...</div>`;
                            break;
                        }
                    }

                    console.log(html);

                    this.popper.setContent({
                        ".popover-body": html
                    });
                }

                if (this.showing == true) {
                    this.popper.show();
                } else {
                    console.log(`popper for ${this.post.id} is to be hidden by now`);
                }
            },

            mouseLeave: async function(): Promise<void> {
                clearTimeout(this.hoverDelay);
                this.showing = false;

                if (this.popper != null) {
                    console.log(`hover leave ${this.post.id}`);
                    this.popper.hide();
                } else {
                    console.log(`hover leave (no popper is up!) ${this.post.id}`);
                }

            }

        },

        beforeDestroy: function(): void {
            if (this.popper != null) {
                this.popper.dispose();
            }
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
