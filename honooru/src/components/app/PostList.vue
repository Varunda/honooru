
<template>
    <div>
        <div v-if="posts.state == 'idle'"></div>

        <div v-else-if="posts.state == 'loading'">
            loading...
        </div>

        <div v-else-if="posts.state == 'loaded'">
            <div style="grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); display: grid; gap: 0.5rem;">
                <div v-for="post in posts.data.results" class="overflow-hidden" style="max-width: 180px;" :class="containerClasses(post)">
                    <span v-if="post.durationSeconds > 0" class="position-absolute font-monospace bg-dark ms-2" style="font-size: 0.8rem;">
                        {{post.durationSeconds | mduration}}
                    </span>

                    <a :href="'/post/' + post.id + '?q=' + q">
                        <img :src="'/media/180x180/' + post.md5 + '.png'" style="width: 180px; height: 180px;"
                             :class="imageClasses(post)" />
                    </a>
                </div>
            </div>

            <div v-if="posts.data.results.length == 0">
                no posts found!
            </div>

            <div v-if="blurUnsafe || blurExplicit" class="text-muted text-center">
                posts with a rating of

                <span v-if="blurExplicit" class="text-danger">
                    explicit
                </span>
                <span v-if="blurExplicit && blurUnsafe">
                    or
                </span>
                <span v-if="blurUnsafe" class="text-warning">
                    unsafe
                </span>

                are blurred because of your <a href="/settings">settings</a> (hover to unblur)
            </div>

            <div v-if="hideUnsafe || hideExplicit" class="text-muted text-center">
                posts with a rating of

                <span v-if="hideExplicit" class="text-danger">
                    explicit
                </span>
                <span v-if="hideExplicit && hideUnsafe">
                    or
                </span>
                <span v-if="hideUnsafe" class="text-warning">
                    unsafe
                </span>

                are not return in this search because of your <a href="/settings">settings</a>
            </div>

            <div class="text-center text-muted font-monospace">
                timings:
                <code v-for="timing in posts.data.timings">
                    {{timing}}
                </code>
            </div>
        </div>

        <div v-else-if="posts.state == 'error'">
            <api-error :error="posts.problem"></api-error>
        </div>

        <div v-else>
            unchecked state of posts: {{posts.state}}
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loading, Loadable } from "Loading";

    import ApiError from "components/ApiError";

    import { Post, SearchResults, PostApi } from "api/PostApi";
    import { UserSetting } from "api/UserSettingApi";

    import "filters/MomentFilter";

    import AccountUtil from "util/AccountUtil";

    export const PostList = Vue.extend({
        props: {
            q: { type: String, required: true },
            limit: { type: Number, required: false, default: 100 }
        },

        data: function() {
            return {
                posts: Loadable.idle() as Loading<SearchResults>
            }
        },

        mounted: function(): void {
            this.search();
        },

        methods: {
            search: async function(): Promise<void> {
                console.log(`searching [query=${this.q}]`);
                this.posts = Loadable.loading();
                this.posts = await PostApi.search(this.q, this.limit);
                this.$emit("search-done", this.posts);
            },

            containerClasses: function(p: Post): string {
                if (p.rating == 2 && this.blurUnsafe) {
                    return "border border-warning rounded";
                }

                if (p.rating == 3 && this.blurExplicit) {
                    return "border border-danger rounded";
                }

                return "";
            },

            imageClasses: function(p: Post) {
                if ((p.rating == 2 && this.blurUnsafe)
                    || (p.rating == 3 && this.blurExplicit)) {

                    return "blurred-image";
                }

                return "";
            }
        },

        watch: {
            q: async function(): Promise<void> {
                this.search();
            }
        },

        computed: {
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
        },

        components: {
            ApiError
        }

    });
    export default PostList;
</script>