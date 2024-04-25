<template>
    <table class="table table-sm">
        <tbody>
            <tr>
                <td>ID</td>
                <td>{{post.id}}</td>
            </tr>

            <tr>
                <td>posted by</td>
                <td>
                    <user-account :account-id="post.posterUserID"></user-account>
                </td>
            </tr>

            <tr>
                <td>status</td>
                <td>
                    <span v-if="post.status == 1" class="text-success">
                        ok
                    </span>
                    <span v-else-if="post.status == 2" class="text-danger">
                        deleted
                    </span>
                    <span v-else class="text-warning">
                        unchecked status: {{post.status}}
                    </span>
                </td>
            </tr>

            <tr>
                <td>timestamp</td>
                <td>{{post.timestamp | moment}}</td>
            </tr>

            <tr>
                <td>file name</td>
                <td class="font-monospace text-break">{{post.fileName}}</td>
            </tr>

            <tr>
                <td>md5</td>
                <td class="font-monospace">
                    <code>
                        {{post.md5}}
                    </code>
                </td>
            </tr>

            <tr>
                <td>file size</td>
                <td class="font-monospace">{{post.fileSizeBytes | bytes}}</td>
            </tr>

            <tr>
                <td>file ext</td>
                <td class="font-monospace">
                    <code>
                        {{post.fileExtension}}
                    </code>
                </td>
            </tr>

            <tr>
                <td>dimensions</td>
                <td class="font-monospace">{{post.width}}x{{post.height}}</td>
            </tr>

            <tr v-if="post.durationSeconds > 0">
                <td>duration</td>
                <td>{{post.durationSeconds | mduration}}</td>
            </tr>
        </tbody>
    </table>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { Post } from "api/PostApi";

    import UserAccount from "components/app/UserAccount.vue";

    import "filters/ByteFilter";
    import "filters/MomentFilter";
    import "filters/LocaleFilter";

    export const PostInfo = Vue.extend({
        props: {
            post: { type: Object as PropType<Post>, required: true }
        },

        components: {
            UserAccount
        }

    });
    export default PostInfo;

</script>