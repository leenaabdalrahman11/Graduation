import { useEffect, useState } from "react";
import { authorizedFetch } from "../../utils/authorizedFetch";
export default function AdminUsers() {

    const [users, setUsers] = useState([]);

    const API = `${import.meta.env.VITE_API_URL}/api/admin/Manage`;


    const loadUsers = async () => {
        try {

            const response = await authorizedFetch(
                `${API}/users`
            );

            const data = await response.json();
console.log(data);

            setUsers(data);

        } catch(error){
            console.log(error);
        }
    };


    useEffect(()=>{
        loadUsers();
    },[]);



    const toggleBlock = async(user)=>{

        const endpoint = user.IsBlocked
            ? `${API}/unblock/${user.Id}`
            : `${API}/block/${user.Id}`;


        await authorizedFetch(endpoint,{
            method:"PATCH"
        });


        loadUsers();
    };


    return (

        <div className="container mt-4">

            <h2 className="mb-4">
                Users Management
            </h2>


            <table className="table table-bordered">

                <thead>

                    <tr>
                        <th>
                            Name
                        </th>

                        <th>
                            Roles
                        </th>

                        <th>
                            Status
                        </th>

                        <th>
                            Actions
                        </th>

                    </tr>

                </thead>


                <tbody>


                {
                    users.map(user=>(

                        <tr key={user.Id}>


                            <td>
                                {user.fullName}
                            </td>


                            <td>

                                {
                                    user.roles[0]
                                }

                            </td>


                            <td>

                                {
                                    user.IsBlocked
                                    ?
                                    <span className="badge bg-danger">
                                        Blocked
                                    </span>
                                    :
                                    <span className="badge bg-success">
                                        Active
                                    </span>
                                }

                            </td>


                            <td>

                                <button
                                className={
                                    user.IsBlocked
                                    ?
                                    "btn btn-success"
                                    :
                                    "btn btn-danger"
                                }
                                onClick={()=>toggleBlock(user)}
                                >

                                {
                                    user.IsBlocked
                                    ?
                                    "Unblock"
                                    :
                                    "Block"
                                }

                                </button>


                            </td>


                        </tr>

                    ))
                }


                </tbody>

            </table>


        </div>

    );
}