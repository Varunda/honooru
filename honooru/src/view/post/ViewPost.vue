<template>
    <div>
        <app-menu class="flex-grow-1">
            <menu-dropdown></menu-dropdown>

            <menu-sep></menu-sep>

            <li class="nav-item h1 p-0">
                Posts
            </li>
        </app-menu>

        <hr class="border" />

        <div v-if="post.state == 'loaded'">
            <file-view :md5="post.data.md5" :file-extension="post.data.fileExtension"></file-view>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { AppMenu, MenuSep, MenuDropdown, MenuImage } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";

    import FileView from "components/app/FileView.vue";

    import { Post, PostApi } from "api/PostApi";

    export const ViewPost = Vue.extend({
        props: {

        },

        data: function() {
            return {
                postID: 0 as number,
                post: Loadable.idle() as Loading<Post>
            }
        },

        mounted: function(): void {
            this.getPostID();
            this.loadPost();
        },

        methods: {
            getPostID: function(): void {
                const parts: string[] = location.pathname.split("/");
                if (parts.length < 3) {
                    console.error(`missing post ID`);
                    return;
                }

                this.postID = Number.parseInt(parts[2]);
            },

            loadPost: async function(): Promise<void> {
                console.log(`loading post ${this.postID}`);
                this.post = Loadable.loading();
                this.post = await PostApi.getByID(this.postID);

            }

        },

        components: {
            InfoHover, ApiError,
            AppMenu, MenuSep, MenuDropdown, MenuImage,
            FileView
        }

    });
    export default ViewPost;

</script>